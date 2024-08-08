using System.Data.Common;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Npgsql;
using Solitons;
using Solitons.CommandLine;
using Solitons.Postgres.PgUp.Models;
using Solitons.Reactive;

namespace Solitons.Postgres.PgUp;

public sealed class PgUpDeploymentHandler
{
    private readonly IProject _project;
    private readonly NpgsqlConnectionStringBuilder _connectionStringBuilder;
    private readonly Dictionary<string, string> _parameters;

    [DebuggerStepThrough]
    private PgUpDeploymentHandler(
        IProject project,
        string connectionString,
        Dictionary<string, string> parameters)
    {
        _project = project;

        _parameters = parameters
            .GroupBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Last().Value,
                StringComparer.OrdinalIgnoreCase);

        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            //Timeout = 120,
            ApplicationName = "PgUp",
            CommandTimeout = Convert.ToInt32(TimeSpan.FromHours(5).TotalSeconds)
        };

        _connectionStringBuilder = builder;
    }


    public static async Task<int> DeployAsync(
        string projectFile,
        string connectionString,
        bool overwrite,
        bool forceOverwrite,
        Dictionary<string, string> parameters,
        TimeSpan? timeout)
    {
        timeout ??= TimeSpan.FromMinutes(5);
        var cancellation = new CancellationTokenSource(timeout.Value).Token;
        var connectionTestTask = Observable
            .Using(
                () => new NpgsqlConnection(connectionString),
                connection => connection
                    .OpenAsync(cancellation)
                    .ToObservable())
            .Catch((ArgumentException e) => Observable.Throw<Unit>(new CliExitException("Invalid connection string")))
            .Catch((FormatException e) => Observable.Throw<Unit>(new CliExitException("Invalid connection string")))
            .WithRetryTrigger(trigger => trigger
                .Where(trigger.Exception is DbException { IsTransient: true })
                .Where(trigger.AttemptNumber < 100)
                .Do(() => Console.WriteLine(trigger.Exception.Message))
                .Do(() => Trace.TraceError(trigger.Exception.ToString()))
                .Delay(TimeSpan
                    .FromMilliseconds(100)
                    .ScaleByFactor(2.0, trigger.AttemptNumber))
                .Do(() => Trace.TraceInformation($"Connection test retry. Attempt: {trigger.AttemptNumber}")))
            .Catch(Observable.Throw<Unit>(new CliExitException("Connection failed")))
            .ToTask(cancellation.WithTimeoutEnforcement(timeout.Value * 0.05));

        connectionString = new NpgsqlConnectionStringBuilder(connectionString)
        {
            ApplicationName = "DbUp",
            CommandTimeout = Convert.ToInt32(TimeSpan.FromHours(10).TotalSeconds)
        }.ConnectionString;

        var loadProjectTask = LoadProjectAsync(projectFile, parameters, cancellation);
        await Task.WhenAll(loadProjectTask, connectionTestTask);
        var project = loadProjectTask.Result;

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        void Print(string key, string value) => Console.WriteLine($"{key}:\t{value}");
        Print("Host", builder.Host!);
        Print("Port", builder.Port.ToString());
        if (overwrite)
        {
            if (false == forceOverwrite)
            {
                var confirmed = CliPrompt.GetYesNoAnswer("Sure? [Y/N]");
                if (!confirmed)
                {
                    throw new CliExitException("Operation cancelled by user")
                    {
                        ExitCode = 0
                    };
                }
            }

            await Observable
                .Using(
                    () => new NpgsqlConnection(connectionString),
                    connection => Observable.FromAsync(() => connection.ExecuteNonQueryAsync(
                        "DROP DATABASE IF EXISTS @dbname FORCE", 
                        cmd => cmd.Parameters.AddWithValue("dbname", project.DatabaseName),
                        cancellation: cancellation)))
                .WithRetryTrigger(trigger => trigger
                    .Where(trigger.Exception is DbException { IsTransient: true })
                    .Where(trigger.AttemptNumber <= 5)
                    .Do(() => Console.WriteLine(trigger.Exception.Message))
                    .Do(() => Trace.TraceError(trigger.Exception.ToString()))
                    .Delay(TimeSpan
                        .FromMilliseconds(100)
                        .ScaleByFactor(2.0, trigger.AttemptNumber))
                    .Do(() => Trace.TraceInformation($"Connection test retry. Attempt: {trigger.AttemptNumber}")))
                .ToTask(cancellation.WithTimeoutEnforcement(timeout.Value / 80));
        }

        var workingDir = new FileInfo(projectFile).Directory ?? new DirectoryInfo(".");
        Debug.Assert(workingDir.Exists);
        var instance = new PgUpDeploymentHandler(project, connectionString, parameters);
        return await instance.DeployAsync(workingDir, cancellation);
    }


    private async Task<int> DeployAsync(
        DirectoryInfo workingDir, 
        CancellationToken cancellation)
    {
        try
        {
            cancellation.ThrowIfCancellationRequested();
            
            await Observable
                .FromAsync(() => CreateDatabaseIfNotExists(_project, cancellation))
                .WithRetryTrigger(trigger => trigger
                    .Where(_ => trigger.Exception is DbException { IsTransient: true })
                    .Where(_ => trigger.AttemptNumber < 100)
                    .Do(_ => Console.WriteLine(trigger.Exception.Message))
                    .Delay(_ => TimeSpan
                        .FromMicroseconds(100)
                        .ScaleByFactor(2, trigger.AttemptNumber)
                        .Max(TimeSpan.FromSeconds(30))))
                .Catch(Observable.Throw<Unit>(new CliExitException(
                    $"Failed to create database {_project.DatabaseName}")));
            cancellation.ThrowIfCancellationRequested();

            var preProcessor = new PgUpScriptPreprocessor(_parameters);

            var pgUpTransactions = _project.GetTransactions(workingDir, preProcessor);
            var connection = new NpgsqlConnection(_connectionStringBuilder
                .WithDatabase(_project.DatabaseName)
                .ConnectionString);
            using var session = Disposable.Create(connection.Dispose);

            connection.Notice += (_, args) => Console.WriteLine(args.Notice.MessageText);
            await connection.OpenAsync(cancellation);

            int transactionCounter = 0;
            foreach (var pgUpTrx in pgUpTransactions)
            {
                transactionCounter++;
                var transactionDisplayName = pgUpTrx.DisplayName.DefaultIfNullOrWhiteSpace(transactionCounter.ToString);
                PgUpTransactionDelimiterRtt.WriteLine(transactionDisplayName);
                await Observable
                    .FromAsync(() => ExecTransaction(pgUpTrx, connection, cancellation))
                    .WithRetryTrigger((trigger) => trigger
                        .Where(_ => trigger.Exception is DbException { IsTransient: true })
                        .Where(_ => trigger.AttemptNumber < 100)
                        .Do(_ => Trace.TraceWarning($"Command failed on attempt {trigger.AttemptNumber}"))
                        .Delay(TimeSpan
                            .FromMilliseconds(200)
                            .ScaleByFactor(1.1, trigger.AttemptNumber)));
            }
            
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            throw new CliExitException("PgUp deployment timeout");
        }
        catch (NpgsqlException e)
        {
            await Console.Error.WriteLineAsync(e.Message);
            return -1;
        }

        return 0;
    }


    async Task ExecTransaction(
        PgUpTransaction pgUpTransaction, 
        NpgsqlConnection connection,
        CancellationToken cancellation)
    {
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        foreach (var stage in pgUpTransaction.GetStages())
        {
            //Console.WriteLine(PgUpResource.PgUpStageAsciiArt);
            var builder = new PgUpCommandBuilder(stage.CustomExecutorInfo);
            foreach (var script in stage.GetScripts())
            {
                Console.WriteLine($@"--- {script.RelativePath}");
                if (script.Content.IsNullOrWhiteSpace())
                {
                    continue;
                }
                await using var command = builder.Build(script.RelativePath, script.Content, connection);
                await command.ExecuteNonQueryAsync(cancellation);
            }
        }

        await transaction.CommitAsync(cancellation);
    }

    private static async Task<IProject> LoadProjectAsync(
        string projectFilePath, 
        Dictionary<string, string> parameters,
        CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        if (false == File.Exists(projectFilePath))
        {
            throw new CliExitException("Specified project file not found.");
        }
        
        try
        {
            var projectFile = new FileInfo(projectFilePath);
            Trace.WriteLine($"Project file: {projectFile.FullName}");
            var pgUpJson = await File.ReadAllTextAsync(projectFile.FullName, cancellation);
            var project = PgUpSerializer.Deserialize(pgUpJson, parameters);
            return project;
        }
        catch (Exception e)
        {
            throw new CliExitException($"Failed to load project file. {e.Message}");
        }
    }
    private async Task CreateDatabaseIfNotExists(
        IProject project, 
        CancellationToken cancellation)
    {
        await using var connection = new NpgsqlConnection(_connectionStringBuilder
            .ConnectionString);
        connection.Notice += (_, args) => Console.WriteLine(args.Notice.MessageText);
        await connection.OpenAsync(cancellation);
        bool databaseExists = await connection.ExecuteScalarAsync<bool>(@$"
        DO $$
        BEGIN
            IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = '{project.DatabaseOwner}') THEN
                CREATE ROLE {project.DatabaseOwner} NOLOGIN;
            END IF;
        END $$;
        GRANT {project.DatabaseOwner} TO CURRENT_USER;
        SELECT EXISTS(SELECT 1 FROM pg_database WHERE datname = '{project.DatabaseName}');
        ", cancellation);
        if(databaseExists)
        {
            await connection.ExecuteNonQueryAsync(
                $"ALTER DATABASE {project.DatabaseName} OWNER TO {project.DatabaseOwner};",
                cancellation);
        }
        else
        {
            await connection.ExecuteNonQueryAsync(
                $"CREATE DATABASE {project.DatabaseName} OWNER {project.DatabaseOwner};",
                cancellation);
        }
    }


    [DebuggerStepThrough]
    public static Task<int> DeployAsync(
        string projectFile,
        string host,
        string username,
        string password,
        Dictionary<string, string> parameters,
        CancellationToken cancellation)
    {

        throw new NotImplementedException();
    }

}
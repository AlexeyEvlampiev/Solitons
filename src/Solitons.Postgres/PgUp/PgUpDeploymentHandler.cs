using System.Data.Common;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Npgsql;
using Solitons.CommandLine;
using Solitons.Postgres.PgUp.Models;

namespace Solitons.Postgres.PgUp;

public sealed class PgUpDeploymentHandler
{
    private readonly string _projectFile;
    private readonly NpgsqlConnectionStringBuilder _connectionStringBuilder;
    private readonly Dictionary<string, string> _parameters;

    [DebuggerStepThrough]
    private PgUpDeploymentHandler(
        string projectFile,
        string connectionString,
        Dictionary<string, string> parameters)
    {
        _projectFile = projectFile;

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


    private async Task<int> DeployAsync(CancellationToken cancellation)
    {
        try
        {
            cancellation.ThrowIfCancellationRequested();
            if (File.Exists(_projectFile) == false)
            {
                throw new CliExitException("Specified PgUp project file not found.");
            }

            var project = await LoadProjectAsync(cancellation);
            var projectFile = new FileInfo(_projectFile);

            await Observable
                .FromAsync(() => CreateDatabaseIfNotExists(project, cancellation))
                .WithRetryTrigger(trigger => trigger
                    .Where(_ => trigger.Exception is DbException { IsTransient: true })
                    .Where(_ => trigger.AttemptNumber < 100)
                    .Do(_ => Console.WriteLine(trigger.Exception.Message))
                    .Delay(_ => TimeSpan
                        .FromMicroseconds(100)
                        .ScaleByFactor(2, trigger.AttemptNumber)
                        .Max(TimeSpan.FromSeconds(30))))
                .Catch(Observable.Throw<Unit>(new CliExitException(
                    $"Failed to create database {project.DatabaseName}")));
            await CreateDatabaseIfNotExists(project, cancellation);
            cancellation.ThrowIfCancellationRequested();

            var workingDir = new DirectoryInfo(projectFile.Directory?.FullName ?? ".");
            var preProcessor = new PgUpScriptPreprocessor(_parameters);

            var pgUpTransactions = project.GetTransactions(workingDir, preProcessor);
            var connection = new NpgsqlConnection(_connectionStringBuilder
                .WithDatabase(project.DatabaseName)
                .ConnectionString);
            using var session = Disposable.Create(connection.Dispose);

            connection.Notice += (_, args) => Console.WriteLine(args.Notice.MessageText);
            await connection.OpenAsync(cancellation);

            int transactionCounter = 0;
            foreach (var pgUpTrx in pgUpTransactions)
            {
                transactionCounter++;
                var transactionDisplayName = pgUpTrx.DisplayName.DefaultIfNullOrWhiteSpace(transactionCounter.ToString);
                Console.WriteLine(PgUpResource.PgUpTransactionAsciiArt);
                Console.WriteLine($@"- {transactionDisplayName}");
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
            Console.WriteLine(PgUpResource.PgUpStageAsciiArt);
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

    async Task<IProject> LoadProjectAsync(CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        
        try
        {
            var projectFile = new FileInfo(_projectFile);
            Trace.WriteLine($"Project file: {projectFile.FullName}");
            var pgUpJson = await File.ReadAllTextAsync(projectFile.FullName, cancellation);
            var project = PgUpSerializer.Deserialize(pgUpJson, _parameters);
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
        
        string connectionString;
        try
        {
            connectionString = new NpgsqlConnectionStringBuilder()
                {
                    Host = host,
                    Username = username,
                    Password = password
                }
                .ConnectionString;
        }
        catch (Exception e)
        {
            throw new CliExitException("Invalid connection information.");
        }
        
        var instance = new PgUpDeploymentHandler(projectFile, connectionString, parameters);
        return instance.DeployAsync(cancellation);
    }

    [DebuggerStepThrough]
    public static Task<int> DeployAsync(
        string projectFile,
        string connectionString,
        Dictionary<string, string> parameters,
        CancellationToken cancellation)
    {
        var instance = new PgUpDeploymentHandler(projectFile, connectionString, parameters);
        return instance.DeployAsync(cancellation);
    }
}
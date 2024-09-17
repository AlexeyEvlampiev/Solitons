using System.Diagnostics;
using System.Reactive.Linq;
using Npgsql;
using Solitons.CommandLine;
using Solitons.Postgres.PgUp.Formatting;
using Solitons.Reactive;

namespace Solitons.Postgres.PgUp;

public sealed class PgUpDatabaseManager
{
    private readonly IPgUpProject _project;
    private readonly IPgUpSession _session;
    private readonly NpgsqlConnectionStringBuilder _connectionStringBuilder;
    private readonly Dictionary<string, string> _parameters;

    [DebuggerStepThrough]
    private PgUpDatabaseManager(
        IPgUpProject project,
        IPgUpSession session,
        NpgsqlConnectionStringBuilder connectionStringBuilder,
        Dictionary<string, string> parameters)
    {
        _project = project;
        _session = session;

        _parameters = parameters
            .GroupBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Last().Value,
                StringComparer.OrdinalIgnoreCase);

        _connectionStringBuilder = connectionStringBuilder;
    }


    public static async Task<int> DeployAsync(
        string projectFile,
        string connectionString,
        bool overwrite,
        bool forceOverwrite,
        Dictionary<string, string> parameters,
        TimeSpan timeout)
    {
        timeout = timeout
            .Min(TimeSpan.FromSeconds(30))
            .Max(TimeSpan.FromHours(24));

        var builder = await FluentObservable
            .Defer(() => NpgsqlConnectionStringParser.Parse(connectionString))
            .Do(builder =>
            {
                builder.ApplicationName = "PgUp";
                builder.Timeout = Convert.ToInt32(timeout.TotalSeconds);
                using var _ = new NpgsqlConnection(builder.ConnectionString);
            })
            .Catch(CliExitException.Observable<NpgsqlConnectionStringBuilder>(
                "Invalid connection string."));
        
        Console.WriteLine(PgUpConnectionDisplayRtt.Build(builder));
        Console.WriteLine();

        try
        {
            var project = await IPgUpProject.LoadAsync(projectFile, parameters);
            IPgUpSession session = new PgUpSession(project.DatabaseOwner, timeout);
            await session.TestConnectionAsync(connectionString);

            
            if (overwrite)
            {
                if (false == forceOverwrite)
                {
                    var confirmed = CliPrompt.GetYesNoAnswer(
                        "This will overwrite the existing database, resulting in complete data loss. " +
                        "Are you sure you want to proceed? (yes/no)");
                    if (!confirmed)
                    {
                        throw new PgUpExitException(0,"Operation cancelled by user");
                    }
                }

                await session.DropDatabaseIfExistsAsync(connectionString, project.DatabaseName);
            }

            var workingDir = new FileInfo(projectFile).Directory ?? new DirectoryInfo(".");
            Debug.Assert(workingDir.Exists);
            var instance = new PgUpDatabaseManager(project, session, builder, parameters);
            return await instance.DeployAsync(workingDir);
        }
        catch (Exception e) when(e is OperationCanceledException ||
                                 e is TaskCanceledException ||
                                 e is TimeoutException)
        {
            await Console.Error.WriteLineAsync("Operation cancelled");
            return 1;
        }
    }


    private async Task<int> DeployAsync(
        DirectoryInfo workingDir)
    {
        try
        {
            await _session.ProvisionDatabaseAsync(
                _connectionStringBuilder.ConnectionString,
                _project.DatabaseName,
                _project.DatabaseOwner);

            var preProcessor = new PgUpScriptPreprocessor(_parameters);

            var pgUpTransactions = _project.GetTransactions(workingDir, preProcessor);
            

            int transactionCounter = 0;
            var connectionString = _connectionStringBuilder
                .WithDatabase(_project.DatabaseName)
                .ConnectionString;
            foreach (var pgUpTrx in pgUpTransactions)
            {
                transactionCounter++;
                var transactionDisplayName = pgUpTrx.DisplayName.DefaultIfNullOrWhiteSpace(transactionCounter.ToString);
                PgUpTransactionDelimiterRtt.WriteLine(transactionDisplayName);
                await _session.ExecuteAsync(pgUpTrx, connectionString);
            }

            return 0;
        }
        catch (OperationCanceledException)
        {
            throw PgUpExitException.DeploymentTimeout();
        }
        catch (NpgsqlException e)
        {
            throw PgUpExitException.FromNpgsqlException(e);
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
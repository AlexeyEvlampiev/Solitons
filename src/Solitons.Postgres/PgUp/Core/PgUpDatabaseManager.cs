using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reactive.Linq;
using Npgsql;
using Solitons.CommandLine;
using Solitons.Postgres.PgUp.CommandLine;
using Solitons.Postgres.PgUp.Core.Formatting;
using Solitons.Reactive;

namespace Solitons.Postgres.PgUp.Core;

public sealed class PgUpDatabaseManager
{
    private const int InitialCountdownValue = 5;
    private const int CountdownDelayMilliseconds = 1000;

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
            .Catch(CliExitException.AsObservable<NpgsqlConnectionStringBuilder>(
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
                Console.WriteLine(PgUpResource.DangerousOperationAscii);
                Console.WriteLine(@$"This action will overwrite the existing database '{project.DatabaseName}', resulting in the permanent loss of all data. ");
                if (false == forceOverwrite)
                {
                    var confirmationMessage = "Are you sure you want to proceed? Type 'yes' to confirm or 'no' to cancel.";
                    bool isConfirmed = CliPrompt.GetYesNoAnswer(confirmationMessage);

                    if (!isConfirmed)
                    {
                        throw PgUpExitException.OperationCancelled();
                    }
                }
                else
                {
                    Console.WriteLine();
                    for (int counter = InitialCountdownValue; counter >= 0; --counter)
                    {
                        Console.Write('\r');
                        Console.Write($@"{counter,-3}");
                        await Task.Delay(CountdownDelayMilliseconds);
                    }
                }

                await session.DropDatabaseIfExistsAsync(connectionString, project.DatabaseName);
            }

            var workingDir = new FileInfo(projectFile).Directory ?? new DirectoryInfo(".");
            Debug.Assert(workingDir.Exists);
            var instance = new PgUpDatabaseManager(project, session, builder, parameters);
            return await instance.DeployAsync(workingDir);
        }
        catch (Exception e) when (e is OperationCanceledException ||
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
            throw PgUpExitException.With(e);
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
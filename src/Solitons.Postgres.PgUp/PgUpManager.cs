using System.Diagnostics;
using Npgsql;
using Solitons.CommandLine;

namespace Solitons.Postgres.PgUp;

public sealed class PgUpManager
{
    private readonly IPgUpProject _project;
    private readonly IPgUpProvider _provider;
    private readonly NpgsqlConnectionStringBuilder _connectionStringBuilder;
    private readonly Dictionary<string, string> _parameters;

    [DebuggerStepThrough]
    private PgUpManager(
        IPgUpProject project,
        IPgUpProvider provider,
        NpgsqlConnectionStringBuilder connectionStringBuilder,
        Dictionary<string, string> parameters)
    {
        _project = project;
        _provider = provider;

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
        timeout = timeout.Min(TimeSpan.FromMinutes(1));
        try
        {
            var builder = IPgUpProvider
                .ParseConnectionString(connectionString)
                .WithApplicationName("PgUp")
                .WithTimeout(timeout);
            Console.WriteLine(PgUpConnectionDisplayRtt.Build(builder));
            Console.WriteLine();

            var project = await IPgUpProject.LoadAsync(projectFile, parameters);

            IPgUpProvider provider = new PgUpSession(timeout);
            await provider.TestConnectionAsync(connectionString);

            
            if (overwrite)
            {
                if (false == forceOverwrite)
                {
                    var confirmed = CliPrompt.GetYesNoAnswer("Sure?");
                    if (!confirmed)
                    {
                        throw new CliExitException("Operation cancelled by user")
                        {
                            ExitCode = 0
                        };
                    }
                }

                await provider.DropDatabaseIfExistsAsync(connectionString, project.DatabaseName);
            }

            var workingDir = new FileInfo(projectFile).Directory ?? new DirectoryInfo(".");
            Debug.Assert(workingDir.Exists);
            var instance = new PgUpManager(project, provider, builder, parameters);
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
            await _provider.ProvisionDatabaseAsync(
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
                await _provider.ExecuteAsync(pgUpTrx, connectionString);
            }

        }
        catch (OperationCanceledException)
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
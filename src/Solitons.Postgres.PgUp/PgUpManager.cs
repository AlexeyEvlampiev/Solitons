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
        IPgUpProvider provider = new PgUpProvider(timeout);
        var cancellation = new CancellationTokenSource(timeout).Token;

        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            ApplicationName = "PgUp",
            CommandTimeout = Convert.ToInt32(timeout.TotalSeconds)
        };

        var loadProjectTask = IPgUpProject.LoadAsync(projectFile, parameters, cancellation);
        await Task.WhenAll(
            loadProjectTask,
            provider.TestConnectionAsync(connectionString));
        var project = loadProjectTask.Result;

        void Print(string key, string value) => Console.WriteLine($@"{key}:	{value}");
        Print("Host", builder.Host!);
        Print("Port", builder.Port.ToString());
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
        return await instance.DeployAsync(workingDir, cancellation);
    }


    private async Task<int> DeployAsync(
        DirectoryInfo workingDir,
        CancellationToken cancellation)
    {
        try
        {
            cancellation.ThrowIfCancellationRequested();

            await _provider.ProvisionDatabaseAsync(
                _connectionStringBuilder.ConnectionString,
                _project.DatabaseName,
                _project.DatabaseOwner);

            cancellation.ThrowIfCancellationRequested();

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
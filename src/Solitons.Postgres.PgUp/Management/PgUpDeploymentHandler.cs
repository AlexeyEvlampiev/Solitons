using System.Data.Common;
using System.Diagnostics;
using System.Reactive.Linq;
using Npgsql;

namespace Solitons.Postgres.PgUp.Management;

internal sealed class PgUpDeploymentHandler
{
    private readonly string _projectFile;
    private readonly string _connectionString;
    private readonly Dictionary<string, string> _parameters;

    [DebuggerStepThrough]
    private PgUpDeploymentHandler(
        string projectFile,
        string connectionString,
        Dictionary<string, string> parameters)
    {
        _projectFile = projectFile;
        _parameters = parameters;

        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            //Timeout = 120,
            ApplicationName = "PgUp",
            CommandTimeout = Convert.ToInt32(TimeSpan.FromHours(5).TotalSeconds)
        };

        _connectionString = builder.ConnectionString;
    }


    private async Task<int> DeployAsync(CancellationToken cancellation)
    {
        try
        {
            cancellation.ThrowIfCancellationRequested();
            if (File.Exists(_projectFile) == false)
            {
                await Console.Error.WriteLineAsync("Specified PgUp project file not found.");
                return -1;
            }

            var projectFile = new FileInfo(_projectFile);
            Trace.WriteLine($"Project file: {projectFile.FullName}");
            var pgUpJson = await File.ReadAllTextAsync(projectFile.FullName, cancellation);
            var project = PgUpSerializer.Deserialize(pgUpJson, _parameters);

            var workingDir = new DirectoryInfo(projectFile.Directory?.FullName ?? ".");
            var preProcessor = new PgUpScriptPreprocessor(_parameters);

            var pgUpTransactions = project.GetTransactions(workingDir, preProcessor);
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellation);
            
            foreach (var pgUpTrx in pgUpTransactions)
            {
                await using var transaction = await connection.BeginTransactionAsync(cancellation);
                foreach (var stage in pgUpTrx.GetStages())
                {
                    var builder = new PgUpCommandBuilder();
                    foreach (var script in stage.GetScripts())
                    {
                        var command = builder.Build(script.RelativePath, script.Content, connection);
                        await Observable
                            .FromAsync(() => command.ExecuteNonQueryAsync(cancellation))
                            .Do(_ => Console.WriteLine())
                            .WithRetryTrigger( (trigger) => trigger
                                .Where(_ => trigger.Exception is DbException {IsTransient: true})
                                .Where(_ => trigger.AttemptNumber < 100)
                                .Do(_ => Trace.TraceWarning($"Command failed on attempt {trigger.AttemptNumber}"))
                                .Delay(TimeSpan
                                    .FromMilliseconds(200)
                                    .ScaleByFactor(1.1, trigger.AttemptNumber)))
                            .Finally(command.Dispose);
                    }
                }

                await transaction.CommitAsync(cancellation);
            }
            
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            Console.WriteLine(@"Operation timeout");
            return -1;
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
        string connectionString,
        Dictionary<string, string> parameters,
        CancellationToken cancellation)
    {
        var instance = new PgUpDeploymentHandler(projectFile, connectionString, parameters);
        return instance.DeployAsync(cancellation);
    }
}
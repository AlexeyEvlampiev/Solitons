using System.Diagnostics;
using Npgsql;

namespace Solitons.Postgres.PgUp.PgUp;

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

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellation);
            
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
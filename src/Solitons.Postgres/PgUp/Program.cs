using System.Diagnostics;
using Npgsql;
using Solitons.CommandLine;
using Solitons.Postgres.PgUp.CommandLine;
using Solitons.Postgres.PgUp.Core;

namespace Solitons.Postgres.PgUp;


public sealed class Program : IPgUp
{
    const string PgUpDescription = "PgUp is a PostgreSQL migration tool using plain SQL for transaction-safe schema changes";

    public static int Main() => CliProcessor
            .Create(ConfigureProcessor)
            .Process();

    private static void ConfigureProcessor(ICliProcessorConfig config)
    {
        config
            .AddService(new Program())
            .WithLogo(PgUpResource.AsciiLogo)
            .WithDescription(PgUpDescription)
            .ConfigGlobalOptions(options => options
                .Clear()
                .Add(new CliTracingGlobalOptionBundle()));
    }


    [DebuggerStepThrough]
    void IPgUp.Initialize(
        string projectDir,
        string template)
    {
        var manager = new PgUpTemplateManager();
        manager.Initialize(projectDir, template);
    }

    Task<int> IPgUp.DeployAsync(
        string projectFile, 
        string host, 
        int port, 
        string username, 
        string password,
        string maintenanceDatabase, 
        Dictionary<string, string> parameters, 
        TimeSpan timeout, 
        CliFlag? overwrite,
        CliFlag? force)
    {
        var csb = new NpgsqlConnectionStringBuilder()
        {
            Host = host,
            Port = port,
            Username = username,
            Password = password,
            Database = maintenanceDatabase
        };
        return PgUpDatabaseManager
            .DeployAsync(
                projectFile,
                csb.ConnectionString,
                overwrite is not null,
                force is not null,
                parameters, timeout);
    }

}

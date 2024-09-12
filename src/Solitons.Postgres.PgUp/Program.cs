using System.Diagnostics;
using System.Reactive;
using Solitons.CommandLine;

namespace Solitons.Postgres.PgUp;


public class Program : IPgUpProgram
{
    private static readonly TimeSpan DefaultActionTimeout = TimeSpan.FromMinutes(10);

    public static int Main()
    {
        return CliProcessor
            .Setup(config => config
                .UseCommandsFrom(new Program())
                .UseLogo(PgUpResource.AsciiLogo)
                .UseDescription("CLI for managing PostgreSQL deployments and tasks using a structured, user-defined sequence of database transactions."))
            .Process();
    }

    [DebuggerStepThrough]
    public void Initialize(
        string projectDir,
        string template)
    {
        PgUpTemplateManager.Initialize(projectDir, template);
    }

    [DebuggerStepThrough]
    public Task<int> DeployAsync(
        string projectFile,
        PgUpConnectionOptionsBundle pgUpConnection,
        Dictionary<string, string>? parameters,
        TimeSpan? timeout)
    {
        return PgUpDatabaseManager
            .DeployAsync(
                projectFile,
                pgUpConnection.ToString(),
                false,
                false,
                parameters ?? [],
                timeout ?? DefaultActionTimeout);
    }


    [DebuggerStepThrough]
    public  Task<int> DeployAsync(
        string projectFile,
        PgUpConnectionOptionsBundle pgUpConnection,
        Unit overwrite,
        Unit? forceOverride,
        Dictionary<string, string>? parameters,
         TimeSpan? timeout)
    {
        return PgUpDatabaseManager
            .DeployAsync(
                projectFile,
                pgUpConnection.ToString(),
                false,
                false,
                parameters ?? [],
                timeout ?? DefaultActionTimeout);
    }

    [DebuggerStepThrough]
    public Task<int> DeployAsync(
        string projectFile,
        string connectionString,
        Dictionary<string, string>? parameters,
        TimeSpan? timeout)
    {
        return PgUpDatabaseManager
            .DeployAsync(
                projectFile,
                connectionString,
                false,
                false,
                parameters ?? [],
                timeout ?? DefaultActionTimeout);
    }

    [DebuggerStepThrough]
    public Task<int> DeployAsync(
        string projectFile,
        string connectionString,
        Unit overwrite,
        Unit? forceOverride,
        Dictionary<string, string>? parameters,
        TimeSpan? timeout)
    {

        return PgUpDatabaseManager
            .DeployAsync(
                projectFile,
                connectionString,
                false,
                false,
                parameters ?? [],
                timeout ?? DefaultActionTimeout);
    }

}

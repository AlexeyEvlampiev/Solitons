using System.Diagnostics;
using System.Reactive;
using Solitons.CommandLine;
using Solitons.Postgres.PgUp.CommandLine;
using Solitons.Postgres.PgUp.Core;

namespace Solitons.Postgres.PgUp;


public sealed class Program() : IPgUpCommandLineContract
{
    const string PgUpDescription = "PgUp is a PostgreSQL migration tool using plain SQL for transaction-safe schema changes";
    private static readonly TimeSpan DefaultActionTimeout = TimeSpan.FromMinutes(10);

    public static int Main() => CliProcessor
            .Create(Initialize)
            .Process();

    private static void Initialize(ICliProcessorConfig config)
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
    void IPgUpCommandLineContract.Initialize(
        string projectDir,
        string template)
    {
        PgUpTemplateManager.Initialize(projectDir, template);
    }

    [DebuggerStepThrough]
    Task<int> IPgUpCommandLineContract.DeployAsync(
        string projectFile,
        PgUpConnectionOptionsBundle pgUpConnection,
        PgUpDeploymentCommonOptionBundle common)
    {
        return PgUpDatabaseManager
            .DeployAsync(
                projectFile,
                pgUpConnection.ToString(),
                false,
                false,
                common.Parameters,
                common.Timeout ?? DefaultActionTimeout);
    }

  


    [DebuggerStepThrough]
    Task<int> IPgUpCommandLineContract.DeployAsync(
        string projectFile,
        PgUpConnectionOptionsBundle pgUpConnection,
        PgUpDeploymentCommonOptionBundle common,
        Unit overwrite,
        Unit? forceOverride)
    {
        return PgUpDatabaseManager
            .DeployAsync(
                projectFile,
                pgUpConnection.ToString(),
                false,
                false,
                common.Parameters,
                common.Timeout ?? DefaultActionTimeout);
    }

    [DebuggerStepThrough]
    Task<int> IPgUpCommandLineContract.DeployAsync(
        string projectFile,
        string connectionString,
        PgUpDeploymentCommonOptionBundle common)
    {
        return PgUpDatabaseManager
            .DeployAsync(
                projectFile,
                connectionString,
                false,
                false,
                common.Parameters,
                common.Timeout ?? DefaultActionTimeout);
    }



    [DebuggerStepThrough]
    public Task<int> DeployAsync(
        string projectFile,
        string connectionString,
        PgUpDeploymentCommonOptionBundle common,
        Unit overwrite,
        Unit? forceOverride)
    {

        return PgUpDatabaseManager
            .DeployAsync(
                projectFile,
                connectionString,
                false,
                false,
                common.Parameters,
                common.Timeout ?? DefaultActionTimeout);
    }

}

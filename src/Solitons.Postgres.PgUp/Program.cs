using System.Diagnostics;
using System.Reactive;
using Solitons.Postgres.PgUp.CommandLine;
using Solitons.Postgres.PgUp.Core;

namespace Solitons.Postgres.PgUp;


public class Program : IProgram
{
    private static readonly TimeSpan DefaultActionTimeout = TimeSpan.FromMinutes(10);

    public static int Main()
    {
        throw new NotImplementedException();
        //return CliProcessorVNext
        //    .Setup(config => config
        //        .UseCommandsFrom(new Program())
        //        .UseLogo(PgUpResource.AsciiLogo)
        //        .UseDescription(IProgram.PgUpDescription))
        //    .Process();
    }

    [DebuggerStepThrough]
    void IProgram.Initialize(
        string projectDir,
        string template)
    {
        PgUpTemplateManager.Initialize(projectDir, template);
    }

    [DebuggerStepThrough]
    Task<int> IProgram.DeployAsync(
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
    Task<int> IProgram.DeployAsync(
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
    Task<int> IProgram.DeployAsync(
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

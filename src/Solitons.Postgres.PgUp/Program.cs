using System.Reactive;
using Solitons.CommandLine;

namespace Solitons.Postgres.PgUp;


internal class Program
{
    private static readonly TimeSpan DefaultActionTimeout = TimeSpan.FromMinutes(10);

    public const string InitializeProjectCommand = "init|initialize";
    public const string ProjectDirectoryArgumentDescription = "File directory where to initialize the new pgup project.";
    public const string InitializeProjectCommandDescription = "Creates a new pgup project structure in the specified directory.";
    public const string TemplateParameterDescription = "The project template to be used.";

    private readonly IPgUpTemplateRepository _templates = new PgUpFileSystemTemplateRepository();


    static int Main()
    {
        return CliProcessor
            .Setup(config => config
                .UseCommandsFrom<Program>()
                .UseLogo(PgUpResource.AsciiLogo))
            .Process();
    }

    [CliCommand("deploy")]
    [CliArgument(nameof(projectFile), "PgUp project file.")]
    public static Task<int> DeployAsync(
        string projectFile,
        [CliOption("--host")] string host,
        [CliOption("--user")] string username,
        [CliOption("--password")] string password,
        [CliOption("--parameter|-p")] Dictionary<string, string>? parameters = null,
        [CliOption("--timeout")] CancellationToken cancellation = default)
    {
        return PgUpDeploymentHandler.DeployAsync(
            projectFile,
            host,
            username, 
            password,
            parameters ?? [],
            cancellation);
    }


    [CliCommand("deploy")]
    [CliArgument(nameof(projectFile), "PgUp project file.")]
    public static Task<int> DeployAsync(
        string projectFile,
        [CliOption("--connection")] string connectionString,
        [CliMapOption("--parameter|-p")] Dictionary<string, string>? parameters = null,
        [CliOption("--timeout")] TimeSpan? timeout = null)
    {
        return PgUpDeploymentHandler
            .DeployAsync(
                projectFile,
                connectionString,
                false,
                false,
                parameters ?? [],
                timeout ?? DefaultActionTimeout);
    }


    [CliCommand("deploy")]
    [CliArgument(nameof(projectFile), "PgUp project file.")]
    public static  Task<int> DeployAsync(
        string projectFile,
        [CliOption("--connection")] string connectionString,
        [CliOption("--overwrite")] Unit overwrite,
        [CliOption("--force")] Unit? forceOverride = null,
        [CliMapOption("--parameter|-p")] Dictionary<string, string>? parameters = null,
        [CliOption("--timeout")] TimeSpan? timeout = null)
    {

        return PgUpDeploymentHandler
            .DeployAsync(
                projectFile,
                connectionString,
                true,
                forceOverride.HasValue,
                parameters ?? [],
                timeout ?? DefaultActionTimeout);
    }
}

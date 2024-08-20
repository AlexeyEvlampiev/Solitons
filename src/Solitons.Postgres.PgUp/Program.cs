using System.ComponentModel;
using System.Reactive;
using Npgsql;
using Solitons.CommandLine;

namespace Solitons.Postgres.PgUp;


public class Program
{
    private static readonly TimeSpan DefaultActionTimeout = TimeSpan.FromMinutes(10);

    public const string DeployCommandDescription = "Deploys a PostgreSQL database according to the pgup.json deployment plan, ensuring all configurations and resources are correctly applied.";
    public const string InitializeProjectCommand = "init|initialize";
    public const string ProjectDirectoryArgumentDescription = "File directory where to initialize the new pgup project.";
    public const string InitializeProjectCommandDescription = "Initializes a new PgUp project structure in the specified directory, setting up the necessary files and folders for PostgreSQL deployment.";
    public const string TemplateParameterDescription = "Specifies the project template to be applied during initialization.";

    private readonly IPgUpTemplateRepository _templates = new PgUpFileSystemTemplateRepository();


    public static int Main()
    {
        return CliProcessor
            .Setup(config => config
                .UseCommandsFrom<Program>()
                .UseLogo(PgUpResource.AsciiLogo)
                .UseDescription("CLI for managing PostgreSQL deployments and tasks using a structured, user-defined sequence of database transactions."))
            .Process();
    }

    [CliCommand("deploy")]
    [CliArgument(nameof(projectFile), "PgUp configuration file.")]
    [Description(DeployCommandDescription)]
    [CliCommandExample("deploy pgup.json --host localhost --username %ADMIN_USR% --password %ADMIN_PWD%")]
    public static Task<int> DeployAsync(
        string projectFile,
        ConnectionBuilderBundle connection,
        [PgUpParametersOption] Dictionary<string, string>? parameters = null,
        [CliOption("--timeout")] TimeSpan? timeout = null)
    {
        return PgUpManager
            .DeployAsync(
                projectFile,
                connection.ToString(),
                false,
                false,
                parameters ?? [],
                timeout ?? DefaultActionTimeout);
    }


    [CliCommand("deploy")]
    [CliArgument(nameof(projectFile), "PgUp configuration file.")]
    [Description(DeployCommandDescription)]
    [CliCommandExample("deploy pgup.json --host localhost --username %ADMIN_USR% --password %ADMIN_PWD% --overwrite")]
    [CliCommandExample("deploy pgup.json --host localhost --username %ADMIN_USR% --password %ADMIN_PWD% --overwrite --force")]
    public static Task<int> DeployAsync(
        string projectFile,
        ConnectionBuilderBundle connection,
        [CliOption("--overwrite")] Unit overwrite,
        [CliOption("--force")] Unit? forceOverride = null,
        [PgUpParametersOption] Dictionary<string, string>? parameters = null,
        [CliOption("--timeout")] TimeSpan? timeout = null)
    {
        return PgUpManager
            .DeployAsync(
                projectFile,
                connection.ToString(),
                true,
                forceOverride.HasValue,
                parameters ?? [],
                timeout ?? DefaultActionTimeout);
    }


    [CliCommand("deploy")]
    [CliArgument(nameof(projectFile), "PgUp project file.")]
    [Description(DeployCommandDescription)]
    public static Task<int> DeployAsync(
        string projectFile,
        [CliOption("--connection")] string connectionString,
        [PgUpParametersOption] Dictionary<string, string>? parameters = null,
        [CliOption("--timeout")] TimeSpan? timeout = null)
    {
        return PgUpManager
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
    [Description(DeployCommandDescription)]
    public static  Task<int> DeployAsync(
        string projectFile,
        [CliOption("--connection")] string connectionString,
        [CliOption("--overwrite")] Unit overwrite,
        [CliOption("--force")] Unit? forceOverride = null,
        [PgUpParametersOption] Dictionary<string, string>? parameters = null,
        [CliOption("--timeout")] TimeSpan? timeout = null)
    {

        return PgUpManager
            .DeployAsync(
                projectFile,
                connectionString,
                true,
                forceOverride.HasValue,
                parameters ?? [],
                timeout ?? DefaultActionTimeout);
    }

    sealed class PgUpParametersOptionAttribute()
        : CliOptionAttribute("--parameter|-p", "Defines parameters for customizing deployment scripts."), ICliMapOption
    {
        public StringComparer GetComparer() => StringComparer.OrdinalIgnoreCase;
    }

    public sealed class ConnectionBuilderBundle : CliOptionBundle
    {
        [CliOption("--host", "Specifies the PostgreSQL server hostname or IP address.")]
        public string Host { get; set; } = "localhost";

        [CliOption("--port", "Specifies the port number on which the PostgreSQL server is listening.")]
        public int Port { get; set; } = 5432;

        [CliOption("--maintenance-database|--mdb", "The name of the maintenance database used for administrative tasks, typically postgres.")]
        public string MaintenanceDatabase { get; set; } = "postgres";

        [CliOption("--username|--user|-usr|-u", "The username to connect to the PostgreSQL maintenance database.")]
        public string Username { get; set; } = "postgres";

        [CliOption("--password|-pwd", "The password associated with the specified PostgreSQL user.")]
        public string Password { get; set; } = "postgres";

        public override  string ToString()
        {
            return new NpgsqlConnectionStringBuilder()
            {
                Host = Host,
                Port = Port,
                Database = MaintenanceDatabase,
                Username = Username,
                Password = Password
            }
            .ConnectionString;
        }
    }
}

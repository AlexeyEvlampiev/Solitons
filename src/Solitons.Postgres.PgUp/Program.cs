using System.ComponentModel;
using System.Diagnostics;
using System.Reactive;
using Npgsql;
using Solitons.CommandLine;

namespace Solitons.Postgres.PgUp;


public class Program
{
    private readonly IPgUpProgram _upProgram;
    private static readonly TimeSpan DefaultActionTimeout = TimeSpan.FromMinutes(10);

    public const string DeployCommandDescription = "Deploys a PostgreSQL database according to the pgup.json deployment plan, ensuring all configurations and resources are correctly applied.";
    public const string InitializeProjectCommand = "init|initialize";
    public const string ProjectDirectoryArgumentDescription = "File directory where to initialize the new pgup project.";
    public const string InitializeProjectCommandDescription = "Initializes a new PgUp project structure in the specified directory, setting up the necessary files and folders for PostgreSQL deployment.";
    public const string TemplateParameterDescription = "Specifies the project template to be applied during initialization.";

    private readonly IPgUpTemplateRepository _templates = new PgUpFileSystemTemplateRepository();

    [DebuggerNonUserCode]
    internal Program(IPgUpProgram upProgram)
    {
        _upProgram = upProgram;
    }

    [DebuggerNonUserCode]
    public Program() : this(new PgUpProgram()) { }


    public static int Main()
    {
        return CliProcessor
            .Setup(config => config
                .UseCommandsFrom(new Program())
                .UseLogo(PgUpResource.AsciiLogo)
                .UseDescription("CLI for managing PostgreSQL deployments and tasks using a structured, user-defined sequence of database transactions."))
            .Process();
    }

    [CliRoute("init|initialize")]
    [CliRouteArgument(nameof(projectDir), "PgUp project directory")]
    public void Initialize(
        string projectDir,
        [CliOption("--template")]string template = "basic")
    {
        _upProgram.Initialize(projectDir, template);
    } 

    [CliRoute("deploy")]
    [CliRouteArgument(nameof(projectFile), "PgUp configuration file.")]
    [Description(DeployCommandDescription)]
    [CliCommandExample("deploy pgup.json --host localhost --username %ADMIN_USR% --password %ADMIN_PWD%", description: "Deploys using the specified admin credentials.")]
    [CliCommandExample("deploy pgup.json --host localhost --username %ADMIN_USR% --password %ADMIN_PWD% --timeout 00:30:00", description: "Deploys with a custom timeout of 30 minutes.")]
    [CliCommandExample("deploy pgup.json --host localhost --port 5432 --management-database postgres --username %ADMIN_USR% --password %ADMIN_PWD% --timeout 00:30:00", description: "Specifies port, management database, and timeout.")]
    [CliCommandExample("deploy pgup.json --host localhost --port 5432 --management-database postgres --username %ADMIN_USR% --password %ADMIN_PWD% --timeout 00:30:00 --parameter[dbName] my_database", description: "Overrides default parameters such as the database name.")]
    public Task<int> DeployAsync(
        string projectFile,
        ConnectionBuilderBundle connection,
        [PgUpParametersOption] Dictionary<string, string>? parameters = null,
        [CliOption("--timeout")] TimeSpan? timeout = null)
    {
        return _upProgram
            .DeployAsync(
                projectFile,
                connection.ToString(),
                false,
                false,
                parameters ?? [],
                timeout ?? DefaultActionTimeout);
    }


    [CliRoute("deploy")]
    [CliRouteArgument(nameof(projectFile), "PgUp configuration file.")]
    [Description(DeployCommandDescription)]
    [CliCommandExample("deploy pgup.json --host localhost --username %ADMIN_USR% --password %ADMIN_PWD%", description: "Deploys the database using the specified admin credentials.")]
    [CliCommandExample("deploy pgup.json --host localhost --username %ADMIN_USR% --password %ADMIN_PWD% --timeout 00:30:00", description: "Deploys with a custom timeout of 30 minutes.")]
    [CliCommandExample("deploy pgup.json --host localhost --port 5432 --management-database postgres --username %ADMIN_USR% --password %ADMIN_PWD% --timeout 00:30:00", description: "Deploys specifying the port, management database, and a custom timeout.")]
    [CliCommandExample("deploy pgup.json --host localhost --port 5432 --management-database postgres --username %ADMIN_USR% --password %ADMIN_PWD% --timeout 00:30:00 --parameter[dbName] my_database", description: "Deploys with a custom database name, overriding the default parameter.")]
    [CliCommandExample("deploy pgup.json --host localhost --username %ADMIN_USR% --password %ADMIN_PWD% --overwrite", description: "Deploys by overwriting the existing database, resulting in the loss of all current data.")]
    [CliCommandExample("deploy pgup.json --host localhost --username %ADMIN_USR% --password %ADMIN_PWD% --overwrite --force", description: "Deploys by forcefully overwriting the existing database without confirmation, resulting in complete data loss.")]
    [CliCommandExample("deploy pgup.json --host localhost --username %ADMIN_USR% --password %ADMIN_PWD% --overwrite --force --parameter[dbOwner] new_owner", description: "Deploys by forcefully overwriting the database, without confirmation, and with a new database owner.")]
    public  Task<int> DeployAsync(
        string projectFile,
        ConnectionBuilderBundle connection,
        [CliOption("--overwrite")] Unit overwrite,
        [CliOption("--force")] Unit? forceOverride = null,
        [PgUpParametersOption] Dictionary<string, string>? parameters = null,
        [CliOption("--timeout")] TimeSpan? timeout = null)
    {
        return _upProgram
            .DeployAsync(
                projectFile,
                connection.ToString(),
                true,
                forceOverride.HasValue,
                parameters ?? [],
                timeout ?? DefaultActionTimeout);
    }


    [CliRoute("deploy")]
    [CliRouteArgument(nameof(projectFile), "PgUp project file.")]
    [Description(DeployCommandDescription)]
    [CliCommandExample("deploy pgup.json --connection \"Host=localhost;Username=postgres;Password=secret\"", description: "Deploys the database using the specified connection string.")]
    [CliCommandExample("deploy pgup.json --connection \"Host=localhost;Username=postgres;Password=secret\" --timeout 00:20:00", description: "Deploys with a custom timeout of 20 minutes using the provided connection string.")]
    [CliCommandExample("deploy pgup.json --connection \"Host=localhost;Username=postgres;Password=secret\" --parameter[dbName] my_database", description: "Deploys with a custom database name, overriding the default parameter.")]
    [CliCommandExample("deploy pgup.json --connection \"Host=localhost;Username=postgres;Password=secret\" --timeout 00:20:00 --parameter[dbOwner] new_owner", description: "Deploys with a custom timeout and a new database owner, overriding the default parameters.")]
    public static Task<int> DeployAsync(
        string projectFile,
        [CliOption("--connection")] string connectionString,
        [PgUpParametersOption] Dictionary<string, string>? parameters = null,
        [CliOption("--timeout")] TimeSpan? timeout = null)
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


    [CliRoute("deploy")]
    [CliRouteArgument(nameof(projectFile), "PgUp project file.")]
    [Description(DeployCommandDescription)]
    [CliCommandExample("deploy pgup.json --connection \"Host=localhost;Username=postgres;Password=secret\" --overwrite", description: "Deploys by overwriting the existing database, resulting in the loss of all current data.")]
    [CliCommandExample("deploy pgup.json --connection \"Host=localhost;Username=postgres;Password=secret\" --overwrite --force", description: "Deploys by forcefully overwriting the existing database without confirmation, leading to complete data loss.")]
    [CliCommandExample("deploy pgup.json --connection \"Host=localhost;Username=postgres;Password=secret\" --overwrite --force --timeout 00:25:00", description: "Deploys with forced overwrite and a custom timeout of 25 minutes.")]
    [CliCommandExample("deploy pgup.json --connection \"Host=localhost;Username=postgres;Password=secret\" --overwrite --force --parameter[dbName] custom_db --timeout 00:25:00", description: "Deploys by forcefully overwriting the existing database, with a custom database name and a 25-minute timeout.")]
    public Task<int> DeployAsync(
        string projectFile,
        [CliOption("--connection")] string connectionString,
        [CliOption("--overwrite")] Unit overwrite,
        [CliOption("--force")] Unit? forceOverride = null,
        [PgUpParametersOption] Dictionary<string, string>? parameters = null,
        [CliOption("--timeout")] TimeSpan? timeout = null)
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

    [CliRoute("template list|ls")]
    [Description(DeployCommandDescription)]
    [CliCommandExample("template list", description: "Displays all project templates.")]
    public static Task<int> ListTemplatesAsync()
    {
        throw new NotImplementedException();
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

        [CliOption("--maintenance-database|-mdb", "The name of the maintenance database used for administrative tasks, typically postgres.")]
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

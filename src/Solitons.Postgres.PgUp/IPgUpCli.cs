using System.ComponentModel;
using System.Reactive;
using Npgsql;
using Solitons.CommandLine;

namespace Solitons.Postgres.PgUp;

public interface IPgUpCli
{
    internal const string PgUpDescription = "A PostgreSQL migration tool focused on simplicity and safety. PgUp uses plain SQL to handle transaction-safe schema changes without additional complexity.";
    const string DeployCommandDescription = "Deploys a PostgreSQL database according to the pgup.json deployment plan, ensuring all configurations and resources are correctly applied.";
    const string InitializeProjectCommand = "init|initialize";
    const string ProjectDirectoryArgumentDescription = "File directory where to initialize the new pgup project.";
    const string InitializeProjectCommandDescription = "Initializes a new PgUp project structure in the specified directory, setting up the necessary files and folders for PostgreSQL deployment.";
    const string TemplateParameterDescription = "Specifies the project template to be applied during initialization.";


    [CliRoute("")]
    [CliCommandExample("--version", "Displays the current version of pgup.")]
    [Description("Displays information about pgup")]
    void ShowVersion(
        [CliOption("--version|-v", "Displays the current version of PgUp.")]CliFlag version);



    [CliRoute(InitializeProjectCommand)]
    [PgUpProjectDirectoryArgument(nameof(projectDir))]
    [Description(InitializeProjectCommandDescription)]
    void Initialize(
        string projectDir,
        [CliOption("--template", "")] string template = "basic");


    [CliRoute("deploy")]
    [CliRouteArgument(nameof(projectFile), "PgUp configuration file.")]
    [CliCommandExample("deploy pgup.json --host localhost --username %ADMIN_USR% --password %ADMIN_PWD%", description: "Deploys using the specified admin credentials.")]
    [CliCommandExample("deploy pgup.json --host localhost --username %ADMIN_USR% --password %ADMIN_PWD% --timeout 00:30:00", description: "Deploys with a custom timeout of 30 minutes.")]
    [CliCommandExample("deploy pgup.json --host localhost --port 5432 --maintenance-database postgres --username %ADMIN_USR% --password %ADMIN_PWD% --timeout 00:30:00", description: "Specifies port, management database, and timeout.")]
    [CliCommandExample("deploy pgup.json --host localhost --port 5432 --maintenance-database postgres --username %ADMIN_USR% --password %ADMIN_PWD% --timeout 00:30:00 --parameter[dbName] my_database", description: "Overrides default parameters such as the database name.")]
    [Description(DeployCommandDescription)]
    Task<int> DeployAsync(
        string projectFile,
        PgUpConnectionOptionsBundle pgUpConnection,
        [PgUpParametersOption] Dictionary<string, string>? parameters = null,
        [CliOption("--timeout")] TimeSpan? timeout = null);


    [CliRoute("deploy")]
    [CliRouteArgument(nameof(projectFile), "PgUp configuration file.")]
    [Description(DeployCommandDescription)]
    [CliCommandExample("deploy pgup.json --host localhost --username %ADMIN_USR% --password %ADMIN_PWD%", description: "Deploys the database using the specified admin credentials.")]
    [CliCommandExample("deploy pgup.json --host localhost --username %ADMIN_USR% --password %ADMIN_PWD% --timeout 00:30:00", description: "Deploys with a custom timeout of 30 minutes.")]
    [CliCommandExample("deploy pgup.json --host localhost --port 5432 --maintenance-database postgres --username %ADMIN_USR% --password %ADMIN_PWD% --timeout 00:30:00", description: "Deploys specifying the port, management database, and a custom timeout.")]
    [CliCommandExample("deploy pgup.json --host localhost --port 5432 --maintenance-database postgres --username %ADMIN_USR% --password %ADMIN_PWD% --timeout 00:30:00 --parameter[dbName] my_database", description: "Deploys with a custom database name, overriding the default parameter.")]
    [CliCommandExample("deploy pgup.json --host localhost --username %ADMIN_USR% --password %ADMIN_PWD% --overwrite", description: "Deploys by overwriting the existing database, resulting in the loss of all current data.")]
    [CliCommandExample("deploy pgup.json --host localhost --username %ADMIN_USR% --password %ADMIN_PWD% --overwrite --force", description: "Deploys by forcefully overwriting the existing database without confirmation, resulting in complete data loss.")]
    [CliCommandExample("deploy pgup.json --host localhost --username %ADMIN_USR% --password %ADMIN_PWD% --overwrite --force --parameter[dbOwner] new_owner", description: "Deploys by forcefully overwriting the database, without confirmation, and with a new database owner.")]
    Task<int> DeployAsync(
        string projectFile,
        PgUpConnectionOptionsBundle pgUpConnection,
        [CliOption("--overwrite")] Unit overwrite,
        [CliOption("--force")] Unit? forceOverride = null,
        [PgUpParametersOption] Dictionary<string, string>? parameters = null,
        [CliOption("--timeout")] TimeSpan? timeout = null);


    [CliRoute("deploy")]
    [CliRouteArgument(nameof(projectFile), "PgUp project file.")]
    [Description(DeployCommandDescription)]
    [CliCommandExample("deploy pgup.json --connection \"Host=localhost;Username=postgres;Password=secret\"", description: "Deploys the database using the specified connection string.")]
    [CliCommandExample("deploy pgup.json --connection \"Host=localhost;Username=postgres;Password=secret\" --timeout 00:20:00", description: "Deploys with a custom timeout of 20 minutes using the provided connection string.")]
    [CliCommandExample("deploy pgup.json --connection \"Host=localhost;Username=postgres;Password=secret\" --parameter[dbName] my_database", description: "Deploys with a custom database name, overriding the default parameter.")]
    [CliCommandExample("deploy pgup.json --connection \"Host=localhost;Username=postgres;Password=secret\" --timeout 00:20:00 --parameter[dbOwner] new_owner", description: "Deploys with a custom timeout and a new database owner, overriding the default parameters.")]
    Task<int> DeployAsync(
        string projectFile,
        [CliOption("--connection")] string connectionString,
        [PgUpParametersOption] Dictionary<string, string>? parameters = null,
        [CliOption("--timeout")] TimeSpan? timeout = null);


    [CliRoute("deploy")]
    [CliRouteArgument(nameof(projectFile), "PgUp project file.")]
    [Description(DeployCommandDescription)]
    [CliCommandExample("deploy pgup.json --connection \"Host=localhost;Username=postgres;Password=secret\" --overwrite", description: "Deploys by overwriting the existing database, resulting in the loss of all current data.")]
    [CliCommandExample("deploy pgup.json --connection \"Host=localhost;Username=postgres;Password=secret\" --overwrite --force", description: "Deploys by forcefully overwriting the existing database without confirmation, leading to complete data loss.")]
    [CliCommandExample("deploy pgup.json --connection \"Host=localhost;Username=postgres;Password=secret\" --overwrite --force --timeout 00:25:00", description: "Deploys with forced overwrite and a custom timeout of 25 minutes.")]
    [CliCommandExample("deploy pgup.json --connection \"Host=localhost;Username=postgres;Password=secret\" --overwrite --force --parameter[dbName] custom_db --timeout 00:25:00", description: "Deploys by forcefully overwriting the existing database, with a custom database name and a 25-minute timeout.")]
    Task<int> DeployAsync(
        string projectFile,
        [CliOption("--connection")] string connectionString,
        [CliOption("--overwrite")] Unit overwrite,
        [CliOption("--force")] Unit? forceOverride = null,
        [PgUpParametersOption] Dictionary<string, string>? parameters = null,
        [CliOption("--timeout")] TimeSpan? timeout = null);
}

sealed class PgUpParametersOptionAttribute()
    : CliOptionAttribute("--parameter|-p", "Defines parameters for customizing deployment scripts.")
{
    public override StringComparer GetValueComparer() => StringComparer.OrdinalIgnoreCase;
}



sealed class PgUpProjectDirectoryArgumentAttribute : CliRouteArgumentAttribute
{
    public PgUpProjectDirectoryArgumentAttribute(string parameterName)
        : base(parameterName, "PgUp project directory.")
    {
        ArgumentRole = "PROJECTDIR";
    }

    public override bool CanAccept(Type argumentType, out TypeConverter converter)
    {
        if (argumentType == typeof(string))
        {
            converter = new StringConverter();
            return true;
        }

        converter = TypeDescriptor.GetConverter(argumentType);
        return false;
    }
}



public sealed class PgUpConnectionOptionsBundle : CliOptionBundle
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

    public override string ToString()
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

using System.ComponentModel;
using Solitons.CommandLine;
using Solitons.CommandLine.Reflection;

/// <summary>
/// Defines the PgUp CLI interface, enabling streamlined project initialization and database deployment.
/// </summary>
public interface IPgUp
{
    /// <summary>
    /// Creates a new PgUp project structure with necessary files for PostgreSQL deployment.
    /// </summary>
    /// <param name="projectDir">The target directory where the PgUp project files and deployment scripts will be created.</param>
    /// <param name="template">Specifies the template used to initialize the project. Default is 'basic'.</param>
	[CliRoute("init|initialize")]
    [CliArgument(nameof(projectDir), description: "The target directory where the PgUp project files and deployment scripts will be created.")]
    [Description("Creates a new PgUp project structure with necessary files for PostgreSQL deployment.")]
    void Initialize(
        string projectDir,
        [CliOption("--template", "Specifies the template used to initialize the project. Default is 'basic'.")] string template = "basic");

    /// <summary>
    /// Deploys a PostgreSQL database using the specified project file and configurations.
    /// </summary>
    /// <param name="projectFile">The PgUp project file defining the database structure and deployment settings.</param>
    /// <param name="host">The hostname or IP address of the PostgreSQL server. Default is 'localhost'.</param>
    /// <param name="port">The port number of the PostgreSQL server. Default is 5432</param>
    /// <param name="username">The PostgreSQL user with permissions to create roles and databases.</param>
    /// <param name="password">The password for the PostgreSQL user.</param>
    /// <param name="maintenanceDatabase">The maintenance database to use during the deployment process. Default is 'postgres'.</param>
    /// <param name="parameters">Key-value pairs to override default parameters in the project file.</param>
    /// <param name="timeout">The maximum time to retry deployment in case of transient errors. Default is '1 minute'.</param>
    /// <param name="overwrite">Drops the existing database and recreates it. All data will be lost.</param>
    /// <param name="force">Suppresses confirmation prompts when overwriting the database. Overrides safety checks.</param>
    /// <returns></returns>
    [CliRoute("deploy|db-deploy")]
    [CliArgument(nameof(projectFile), "The PgUp project file defining the database structure and deployment settings.")]
    [CliCommandExample("deploy pgup.json --host localhost --username %ADMIN_USR% --password %ADMIN_PWD%", description: "Deploys using the specified admin credentials.")]
    [CliCommandExample("deploy pgup.json --host localhost --username %ADMIN_USR% --password %ADMIN_PWD% --timeout 00:30:00", description: "Deploys with a custom timeout of 30 minutes.")]
    [CliCommandExample("deploy pgup.json --host localhost --port 5432 --maintenance-database postgres --username %ADMIN_USR% --password %ADMIN_PWD% --timeout 00:30:00", description: "Specifies port, management database, and timeout.")]
    [CliCommandExample("deploy pgup.json --host localhost --port 5432 --maintenance-database postgres --username %ADMIN_USR% --password %ADMIN_PWD% --timeout 00:30:00 --parameter[dbName] my_database", description: "Overrides default parameters such as the database name.")]
    [Description("Deploys a PostgreSQL database using the specified project file and configurations.")]
    Task<int> DeployAsync(
        string projectFile,
        [CliOption("--host|-h", "The hostname or IP address of the PostgreSQL server. Default is 'localhost'.", Default = "localhost")]string host,
        [CliOption("--port|-p", "The hostname or IP address of the PostgreSQL server. Default is 'localhost'.", Default = "5432")]int port,
        [CliOption("--username|--user|-u", "The PostgreSQL user with permissions to create roles and databases.")]string username,
        [CliOption("--password|--pwd|-p", "The password for the PostgreSQL user.")]string password,
        [CliOption("--maintenance-database|-mdb", "The maintenance database to use during the deployment process. Default is 'postgres'.", Default = "postgres")]string maintenanceDatabase,
        [CliOption("--parameters|--parameter|-param", "Key-value pairs to override default parameters in the project file.")]Dictionary<string, string> parameters,
        [CliOption("--timeout", "The maximum time to retry deployment in case of transient errors. Default is '1 minute'.", Default = "1 minute")]TimeSpan timeout,
        [CliOption("--overwrite", "Drops the existing database and recreates it. All data will be lost.")]CliFlag? overwrite = null,
        [CliOption("--force", "Suppresses confirmation prompts when overwriting the database. Overrides safety checks.")]CliFlag? force = null);
}

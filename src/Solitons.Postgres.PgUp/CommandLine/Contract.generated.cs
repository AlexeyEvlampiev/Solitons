using System.ComponentModel;
using Solitons.CommandLine;
using Solitons.CommandLine.Reflection;

/// <summary>
/// Defines the pgup CLI interface.
/// </summary>
public interface IPgUp
{
    /// <summary>
    /// Sets up a new PgUp project structure with required files and folders for PostgreSQL deployment in the specified directory
    /// </summary>
    /// <param name="projectDir">Directory to populate with the pgup.json project file and deployment sql scripts.</param>
    /// <param name="template">The template to initialize the new project from</param>
	[CliRoute("init|initialize")]
    [CliArgument(nameof(projectDir), description: "Directory to populate with the pgup.json project file and deployment sql scripts.")]
    [Description("Sets up a new PgUp project structure with required files and folders for PostgreSQL deployment in the specified directory")]
    void Initialize(
        string projectDir,
        [CliOption("--template", "The template to initialize the new project from")] string template = "basic");

    /// <summary>
    /// Deployes database using the given database project file and given deployment configurations,
    /// </summary>
    /// <param name="projectFile">Database project file to use for the deployment.</param>
    /// <param name="host">Postgres server host.</param>
    /// <param name="port">Postgres server port.</param>
    /// <param name="username">Postgres user who is authorized creating new roles on the server and creating new databases.</param>
    /// <param name="password">Postgres user password to authnticate to the server.</param>
    /// <param name="maintenanceDatabase">Postgres maintenance database.</param>
    /// <param name="parameters">Parameters -  key value pairs to override parameters defined in the database project file..</param>
    /// <param name="timout">The timeout the deployment may retry within. Only transient errors are subject of retry.</param>
    /// <param name="overwrite">Flag specifying whether the existing database should be dropped and recreted from sctartch. When applied, all data will be lost..</param>
    /// <param name="forse">Flag specifying whether the user should not be prompted when override is specified. If specified, the database will be dropped and redeployed anyway.</param>
    /// <returns></returns>
    [CliRoute("deploy")]
    [CliArgument(nameof(projectFile), "Database project file to use for the deployment.")]
    [CliCommandExample("deploy pgup.json --host localhost --username %ADMIN_USR% --password %ADMIN_PWD%", description: "Deploys using the specified admin credentials.")]
    [CliCommandExample("deploy pgup.json --host localhost --username %ADMIN_USR% --password %ADMIN_PWD% --timeout 00:30:00", description: "Deploys with a custom timeout of 30 minutes.")]
    [CliCommandExample("deploy pgup.json --host localhost --port 5432 --maintenance-database postgres --username %ADMIN_USR% --password %ADMIN_PWD% --timeout 00:30:00", description: "Specifies port, management database, and timeout.")]
    [CliCommandExample("deploy pgup.json --host localhost --port 5432 --maintenance-database postgres --username %ADMIN_USR% --password %ADMIN_PWD% --timeout 00:30:00 --parameter[dbName] my_database", description: "Overrides default parameters such as the database name.")]
    [Description("Deployes database using the given database project file and given deployment configurations,")]
    Task<int> DeployAsync(
        string projectFile,
        [CliOption("--host|-h", "Postgres server host.")]string host,
        [CliOption("--port|-p", "Postgres server host.")]int port,
        [CliOption("--username|--user|-u", "Postgres user who is authorized creating new roles on the server and creating new databases.")]string username,
        [CliOption("--password|--pwd|-p", "Postgres user password to authnticate to the server.")]string password,
        [CliOption("--maintenance-database|-mdb", "Postgres maintenance database.")]string maintenanceDatabase,
        [CliOption("--parameters|--parameter|-param", "Parameters -  key value pairs to override parameters defined in the database project file..")]Dictionary<string, string> parameters,
        [CliOption("--timeout", "The timeout the deployment may retry within. Only transient errors are subject of retry.")]TimeSpan timout,
        [CliOption("--overwrite", "Flag specifying whether the existing database should be dropped and recreted from sctartch. When applied, all data will be lost..")]CliFlag? overwrite,
        [CliOption("--forse", "Deployes database using the given database project file and given deployment configurations,")]CliFlag? forse);

}

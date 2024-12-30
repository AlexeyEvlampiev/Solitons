
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
    [CliCommandExample("init ./pgup_project --template basic", "Initializes a PgUp project in the directory './pgup_project' using the default 'basic' template.")]  
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
    [CliCommandExample("deploy pgup.json --host localhost --username pgup_admin --password postgres --parameters.databasePrefix dev --parameters.databaseAdminPassword dev_password", "Deploys a database to the local server with a 'dev_' prefix for the database name. For a core database name defined in 'pgup.json' as 'mydb', the resulting database name will be 'dev_mydb'. Uses 'pgup_admin' as the admin user to create roles and databases. The database owner connects with the login defined in 'pgup.json' and the password 'dev_password'.")]  
    [CliCommandExample("deploy pgup.json --host 192.168.1.10 --port 5432 --username admin --password secure123 --parameters.databasePrefix prod_", "Deploys the database to a remote PostgreSQL server at 192.168.1.10. The database name will have a 'prod_' prefix, e.g., 'prod_mydb', as per the parameter override.")]  
    [CliCommandExample("deploy pgup.json --host localhost --username admin --password postgres --overwrite --force", "Re-deploys the database, overwriting the existing one without confirmation prompts. All previous data in the database will be lost.")]  
    [CliCommandExample("deploy pgup.json --username pgup_admin --password postgres --parameters.databaseOwner new_owner --parameters.databaseName new_database", "Deploys a database with a custom owner and database name, overriding the default values in 'pgup.json'.")]  
    [CliCommandExample("deploy pgup.json --timeout 00:02:00 --username pgup_admin --password postgres", "Deploys the database with a custom timeout of 2 minutes for retrying transient errors.")]  
    [Description("Deploys a PostgreSQL database using the specified project file and configurations.")]
    Task<int> DeployAsync(
        string projectFile,
        [CliOption("--host|-h", "The hostname or IP address of the PostgreSQL server. Default is 'localhost'.", DefaultValue = "localhost")]string host,
        [CliOption("--port|-p", "The hostname or IP address of the PostgreSQL server. Default is 'localhost'.", DefaultValue = "5432")]int port,
        [CliOption("--username|--user|-u", "The PostgreSQL user with permissions to create roles and databases.")]string username,
        [CliOption("--password|--pwd|-p", "The password for the PostgreSQL user.")]string password,
        [CliOption("--maintenance-database|-mdb", "The maintenance database to use during the deployment process. Default is 'postgres'.", DefaultValue = "postgres")]string maintenanceDatabase,
        [CliOption("--parameters|--parameter|-param", "Key-value pairs to override default parameters in the project file.")]Dictionary<string, string> parameters,
        [CliOption("--timeout", "The maximum time to retry deployment in case of transient errors. Default is '1 minute'.", DefaultValue = "1 minute")]TimeSpan timeout,
        [CliOption("--overwrite", "Drops the existing database and recreates it. All data will be lost.")]CliFlag? overwrite = null,
        [CliOption("--force", "Suppresses confirmation prompts when overwriting the database. Overrides safety checks.")]CliFlag? force = null);
}

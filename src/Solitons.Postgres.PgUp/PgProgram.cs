namespace Solitons.Postgres.PgUp;

internal class PgUpProgram : IPgUpProgram
{
    public void Initialize(string projectDir, string template)
    {
        PgUpTemplateManager.Initialize(projectDir, template);
    }

    public Task<int> DeployAsync(
        string projectFile, 
        string connectionString, 
        bool overwrite, 
        bool forceOverwrite, 
        Dictionary<string, string> parameters,
        TimeSpan timeout)
    {
        return PgUpDatabaseManager
            .DeployAsync(
                projectFile,
                connectionString,
                false,
                false,
                parameters ?? [],
                timeout);
    }
}
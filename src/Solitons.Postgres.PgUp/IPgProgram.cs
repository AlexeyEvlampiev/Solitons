namespace Solitons.Postgres.PgUp;

internal interface IPgUpProgram
{
    void Initialize(
        string projectDir, 
        string template);

    Task<int> DeployAsync(
        string projectFile, 
        string toString, 
        bool overwrite, 
        bool forceOverwrite, 
        Dictionary<string, string> parameters, 
        TimeSpan timeout);
}
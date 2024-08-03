namespace Solitons.Postgres.PgUp.Management;

public interface IPgUpStage
{
    IEnumerable<string> GetScriptFiles();
    string GetWorkingDirectory();

    bool HasCustomExecutor(out IPgUpCustomExecutor customExecutor);
}
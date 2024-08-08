namespace Solitons.Postgres.PgUp;

public interface IPgUpStage
{
    IEnumerable<string> GetScriptFiles();
    string GetWorkingDirectory();

    bool HasCustomExecutor(out IPgUpCustomExecutor customExecutor);
}
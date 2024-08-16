namespace Solitons.Postgres.PgUp;

public interface IPgUpBatch
{
    IEnumerable<string> GetScriptFiles();
    string GetWorkingDirectory();

    string? GetCustomExecCommandText();

}
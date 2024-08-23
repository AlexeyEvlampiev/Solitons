using Solitons.Postgres.PgUp.Models;

namespace Solitons.Postgres.PgUp;

public interface IPgUpBatch
{
    IEnumerable<string> GetRunOrder();
    string GetWorkingDirectory();
    string? GetCustomExecCommandText();
    FileDiscoveryMode GetFileDiscoveryMode();

}
using Solitons.Postgres.PgUp.Core.Models;

namespace Solitons.Postgres.PgUp.Core;

public interface IPgUpBatch
{
    IEnumerable<string> GetRunOrder();
    string GetWorkingDirectory();
    string? GetCustomExecCommandText();
    PgUpScriptDiscoveryMode GetFileDiscoveryMode();

}
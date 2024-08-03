

namespace Solitons.Postgres.PgUp.Management;

public interface IPgUpCustomExecutor
{
    string GetFilePathParameterName();
    string GetFileContentParameterName();
    string GetCommandText();
}
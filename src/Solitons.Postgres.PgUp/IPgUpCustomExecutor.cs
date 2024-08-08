

namespace Solitons.Postgres.PgUp;

public interface IPgUpCustomExecutor
{
    string GetFilePathParameterName();
    string GetFileContentParameterName();
    string GetCommandText();
}
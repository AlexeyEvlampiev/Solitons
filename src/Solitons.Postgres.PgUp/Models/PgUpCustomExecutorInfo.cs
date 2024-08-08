namespace Solitons.Postgres.PgUp.Models;

public sealed record PgUpCustomExecutorInfo
{
    public PgUpCustomExecutorInfo(IPgUpCustomExecutor customExecutor)
    {
        FilePathParameterName = customExecutor.GetFilePathParameterName();
        FileContentParametersName = customExecutor.GetFileContentParameterName();
        CommandText = customExecutor.GetCommandText();
    }

    public string CommandText { get; }

    public string FileContentParametersName { get; }

    public string FilePathParameterName { get;  }
}
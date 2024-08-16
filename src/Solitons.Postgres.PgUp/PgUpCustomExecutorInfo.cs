namespace Solitons.Postgres.PgUp;

public sealed record PgUpCustomExecutorInfo
{
    public PgUpCustomExecutorInfo(IPgUpCustomExecutor customExecutor)
    {
        FilePathParameterName = customExecutor.GetFilePathParameterName();
        FileContentParametersName = customExecutor.GetFileContentParameterName();
        FileChecksumParameterName = customExecutor.GetFileChecksumParameterName();
        CommandText = customExecutor.GetCommandText();
    }

    public string CommandText { get; }
    public string FileContentParametersName { get; }
    public string FilePathParameterName { get; }
    public string FileChecksumParameterName { get; }
}
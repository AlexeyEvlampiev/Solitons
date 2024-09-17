using Npgsql;
using Solitons.CommandLine;

namespace Solitons.Postgres.PgUp;

internal sealed class PgUpExitException : CliExitException
{
    internal PgUpExitException(string message) : base(message)
    {
    }

    internal PgUpExitException(int exitCode, string message) : base(message)
    {
        ExitCode = exitCode;
    }

    public static PgUpExitException DeploymentTimeout() => new("PgUp deployment timeout");

    public static PgUpExitException FromNpgsqlException(NpgsqlException exception) => new(exception.Message);

    public static PgUpExitException ProjectFileNotFound(string projectFilePath) => new PgUpExitException("Specified PgUp project file does not exist.");
}
using Npgsql;
using Solitons.CommandLine;

namespace Solitons.Postgres.PgUp.Core;

internal sealed class PgUpExitException(string message) : CliExitException(message)
{
    public static PgUpExitException DeploymentTimeout() => new("PgUp deployment timeout");

    public static PgUpExitException With(NpgsqlException exception) => new(exception.Message);

    public static PgUpExitException ProjectFileNotFound(string projectFilePath) => new("Specified PgUp project file does not exist.");

    public static PgUpExitException FailedToLoadProjectFile(string projectFilePath, string eMessage)
    {
        throw new NotImplementedException();
    }


    public static PgUpExitException OperationCancelled()
    {
        throw new NotImplementedException();
    }

    public static PgUpExitException InvalidConnectionString(Exception innerException)
    {
        return new($"Invalid connection string. {innerException.Message}");
    }
}
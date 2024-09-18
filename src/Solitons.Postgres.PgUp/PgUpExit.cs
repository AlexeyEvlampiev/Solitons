using System.Diagnostics;
using Npgsql;
using Solitons.CommandLine;

namespace Solitons.Postgres.PgUp;

internal static class PgUpExit
{
    public static Exception DeploymentTimeout() => With("PgUp deployment timeout");

    public static Exception With(NpgsqlException exception) => With(exception.Message);

    public static Exception ProjectFileNotFound(string projectFilePath) => With("Specified PgUp project file does not exist.");

    public static Exception FailedToLoadProjectFile(string projectFilePath, string eMessage)
    {
        throw new NotImplementedException();
    }

    [DebuggerStepThrough]
    public static Exception With(string message) => CliExit.Raise(message);

    public static Exception OperationCancelled()
    {
        throw new NotImplementedException();
    }
}
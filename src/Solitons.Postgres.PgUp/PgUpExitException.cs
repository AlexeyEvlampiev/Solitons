using Npgsql;
using Solitons.CommandLine;

namespace Solitons.Postgres.PgUp;

internal sealed class PgUpExitException : CliExitException
{
    internal PgUpExitException(string message) : base(message)
    {
    }

    public static PgUpExitException DeploymentTimeout() => new("PgUp deployment timeout");

    public static PgUpExitException FromNpgsqlException(NpgsqlException exception) => new(exception.Message);
}
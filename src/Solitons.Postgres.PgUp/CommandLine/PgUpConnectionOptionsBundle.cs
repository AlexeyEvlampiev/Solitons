using Npgsql;
using Solitons.CommandLine;
using Solitons.CommandLine.Reflection;

namespace Solitons.Postgres.PgUp.CommandLine;

public sealed class PgUpConnectionOptionsBundle : CliOptionBundle
{
    [CliOption("--host", "Specifies the PostgreSQL server hostname or IP address.")]
    public string Host { get; set; } = "localhost";

    [CliOption("--port", "Specifies the port number on which the PostgreSQL server is listening.")]
    public int Port { get; set; } = 5432;

    [CliOption("--maintenance-database|-mdb", "The name of the maintenance database used for administrative tasks, typically postgres.")]
    public string MaintenanceDatabase { get; set; } = "postgres";

    [CliOption("--username|--user|-usr|-u", "The username to connect to the PostgreSQL maintenance database.")]
    public string Username { get; set; } = "postgres";

    [CliOption("--password|-pwd", "The password associated with the specified PostgreSQL user.")]
    public string Password { get; set; } = "postgres";

    public override string ToString()
    {
        return new NpgsqlConnectionStringBuilder()
            {
                Host = Host,
                Port = Port,
                Database = MaintenanceDatabase,
                Username = Username,
                Password = Password
            }
            .ConnectionString;
    }
}
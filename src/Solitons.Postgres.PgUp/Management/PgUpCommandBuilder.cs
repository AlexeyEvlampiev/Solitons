

using Npgsql;

namespace Solitons.Postgres.PgUp.Management;

public class PgUpCommandBuilder
{
    public virtual NpgsqlCommand Build(
        string filePath, 
        string commandText, 
        NpgsqlConnection connection)
    {
        return new NpgsqlCommand(commandText, connection);
    }
}
using System.Diagnostics;
using Npgsql;

namespace Solitons.Postgres;

public static class Extensions
{
    public static NpgsqlConnectionStringBuilder WithDatabase(
        this NpgsqlConnectionStringBuilder self,
        string database)
    {
        return new NpgsqlConnectionStringBuilder(self.ConnectionString)
        {
            Database = database
        };
    }

    [DebuggerStepThrough]
    public static async Task<int> ExecuteNonQueryAsync(
        this NpgsqlConnection connection,
        string commandText, 
        Action<NpgsqlCommand> config, 
        CancellationToken cancellation = default)
    {
        await using var command = new NpgsqlCommand(commandText, connection);
        config.Invoke(command);
        return await command.ExecuteNonQueryAsync(cancellation);
    }
}
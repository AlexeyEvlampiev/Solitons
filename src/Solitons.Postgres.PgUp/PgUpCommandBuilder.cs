using Npgsql;
using Solitons.Postgres.PgUp.Models;

namespace Solitons.Postgres.PgUp;

public sealed class PgUpCommandBuilder(PgUpCustomExecutorInfo? customExecutorInfo)
{
    //[DebuggerStepThrough]
    public NpgsqlCommand Build(
        string filePath, 
        string commandText, 
        NpgsqlConnection connection)
    {
        if (customExecutorInfo != null)
        {
            var command = new NpgsqlCommand(customExecutorInfo.CommandText, connection);
            command.Parameters.AddWithValue(customExecutorInfo.FilePathParameterName, filePath);
            command.Parameters.AddWithValue(customExecutorInfo.FileContentParametersName, commandText);
            return command;
        }

        return new NpgsqlCommand(commandText, connection);
    }
}
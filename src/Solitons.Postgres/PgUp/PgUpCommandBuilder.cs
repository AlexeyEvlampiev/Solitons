using System.Diagnostics;
using Npgsql;
using Solitons.Postgres.PgUp.Models;

namespace Solitons.Postgres.PgUp;

public sealed class PgUpCommandBuilder
{
    private readonly PgUpCustomExecutorInfo? _customExecutorInfo;


    public PgUpCommandBuilder(PgUpCustomExecutorInfo? customExecutorInfo)
    {
        _customExecutorInfo = customExecutorInfo;
    }

    //[DebuggerStepThrough]
    public NpgsqlCommand Build(
        string filePath, 
        string commandText, 
        NpgsqlConnection connection)
    {
        if (_customExecutorInfo != null)
        {
            var command = new NpgsqlCommand(_customExecutorInfo.CommandText, connection);
            command.Parameters.AddWithValue(_customExecutorInfo.FilePathParameterName, filePath);
            command.Parameters.AddWithValue(_customExecutorInfo.FileContentParametersName, commandText);
            return command;
        }

        return new NpgsqlCommand(commandText, connection);
    }
}
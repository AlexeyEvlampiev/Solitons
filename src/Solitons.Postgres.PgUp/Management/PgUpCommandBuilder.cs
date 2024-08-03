

using System.Diagnostics;
using Npgsql;
using Solitons.Postgres.PgUp.Management.Models;

namespace Solitons.Postgres.PgUp.Management;

public sealed class PgUpCommandBuilder
{
    private readonly PgUpCustomExecutorInfo? _customExecutorInfo;


    public PgUpCommandBuilder(PgUpCustomExecutorInfo? customExecutorInfo)
    {
        _customExecutorInfo = customExecutorInfo;
    }

    [DebuggerStepThrough]
    public NpgsqlCommand Build(
        string filePath, 
        string commandText, 
        NpgsqlConnection connection)
    {
        if (_customExecutorInfo != null)
        {
            var command = new NpgsqlCommand(commandText, connection);
            command.Parameters.AddWithValue(_customExecutorInfo.FilePathParameterName, filePath);
            command.Parameters.AddWithValue(_customExecutorInfo.FileContentParametersName, commandText);
            return command;
        }

        return new NpgsqlCommand(commandText, connection);
    }
}
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Npgsql;
using NpgsqlTypes;
using Solitons.CommandLine;

namespace Solitons.Postgres.PgUp;

public sealed class PgUpCommandBuilder
{
    private readonly string? _customExecCommandText;
    private readonly string _parameterName;

    public PgUpCommandBuilder(string? customExecCommandText)
    {
        _customExecCommandText = customExecCommandText;
        
        if (customExecCommandText.IsPrintable())
        {
            var matches = Regex.Matches(customExecCommandText ?? "", @"(?<=@)\w+");
            if (matches.Count == 1)
            {
                _parameterName = matches.Single().Value;
            }
            else if (matches.Count == 0)
            {
                throw new PgUpExitException(
                    "Custom script execution command must have a single parameter " +
                    "that is JSON containing the script content to be executed and script metadata " +
                    "such as file path and script checksum.");
            }
            else
            {
                throw new PgUpExitException(
                    "Custom script execution command must have a single parameter " +
                    "that is JSON containing the script content to be executed and script metadata " +
                    $"such as file path and script checksum. Actual: {matches.Count}");
            }
        }
    }

    //[DebuggerStepThrough]
    public NpgsqlCommand Build(
        string filePath, 
        string commandText, 
        string checksum,
        NpgsqlConnection connection)
    {
        if (_customExecCommandText.IsPrintable())
        {
            var json = JsonSerializer.Serialize(new CustomExecPayload
            {
                FilePath = filePath,
                FileContent = commandText,
                Checksum = checksum
            });
            var command = new NpgsqlCommand(_customExecCommandText, connection);
            command.Parameters.AddWithValue(_parameterName, NpgsqlDbType.Jsonb, json);
            return command;
        }

        return new NpgsqlCommand(commandText, connection);
    }

    public sealed class CustomExecPayload
    {
        [JsonPropertyName("filePath"), JsonRequired]
        public string? FilePath { get; set; }

        [JsonPropertyName("command"), JsonRequired]
        public string? FileContent { get; set; }

        [JsonPropertyName("checksum"), JsonRequired]
        public string? Checksum { get; set; }
    }
}
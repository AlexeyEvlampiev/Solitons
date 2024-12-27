using System.Text.RegularExpressions;
using Npgsql;

namespace Solitons.Postgres;

public static class NpgsqlConnectionStringParser
{
    // Compiled regex for performance improvement
    private static readonly Regex JdbcRegex = new Regex(
        @"jdbc:postgresql://(?<host>[^/:]+)(?::(?<port>\d+))?/(?<database>[^?]+)(?<params>\?.*)?",
        RegexOptions.ExplicitCapture | RegexOptions.Compiled);

    public static NpgsqlConnectionStringBuilder Parse(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string cannot be null or whitespace.", nameof(connectionString));
        }

        if (connectionString.StartsWith("jdbc:postgresql://"))
        {
            connectionString = ConvertJdbcToNpgsql(connectionString);
        }

        return new NpgsqlConnectionStringBuilder(connectionString);
    }

    private static string ConvertJdbcToNpgsql(string jdbcString)
    {
        var match = JdbcRegex.Match(jdbcString);
        if (!match.Success)
        {
            throw new ArgumentException("Invalid JDBC connection string format.", nameof(jdbcString));
        }

        var host = match.Groups["host"].Value;
        var port = match.Groups["port"].Success ? $"Port={match.Groups["port"].Value};" : string.Empty;
        var database = match.Groups["database"].Value;
        var additionalParams = TransformJdbcParamsToNpgsql(match.Groups["params"].Value);

        return $"Host={host};{port}Database={database};" + additionalParams;
    }

    private static string TransformJdbcParamsToNpgsql(string jdbcParams)
    {
        if (string.IsNullOrEmpty(jdbcParams) || !jdbcParams.StartsWith("?"))
        {
            return string.Empty;
        }

        // Remove the leading '?' and transform params
        var parameters = jdbcParams.Substring(1);
        var transformed = Regex.Replace(parameters, @"([^&=]+)=([^&=]*)", m => $"{m.Groups[1].Value}={m.Groups[2].Value};");
        return transformed.Replace("&", ";");
    }
}
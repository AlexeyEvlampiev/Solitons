using Npgsql;

namespace Solitons.Postgres.PgUp;

public static class EnvironmentInfo
{
    public const string ConnectionStringKey = "SOLITONS_TEST_POSTGRES_SERVER_CONNECTION_STRING";

    public static string? ConnectionString => Environment.GetEnvironmentVariable(ConnectionStringKey);

    public static async Task TestConnectionAsync()
    {
        Assert.True(ConnectionString.IsPrintable(), $"{ConnectionStringKey} environment variable is missing.");
        try
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await connection.CloseAsync();
        }
        catch (Exception e)
        {
            Assert.Fail($"Could not connected to the test postgres server. {e.Message}");
        }
        
    }
}
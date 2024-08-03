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
}
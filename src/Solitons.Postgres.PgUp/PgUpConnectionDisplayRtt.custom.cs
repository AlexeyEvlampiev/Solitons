using Npgsql;

namespace Solitons.Postgres.PgUp;

internal partial class PgUpConnectionDisplayRtt
{
    private readonly NpgsqlConnectionStringBuilder _builder;

    private PgUpConnectionDisplayRtt(NpgsqlConnectionStringBuilder builder)
    {
        _builder = builder;
    }

    public static string Build(NpgsqlConnectionStringBuilder builder)
    {
        var rtt = new PgUpConnectionDisplayRtt(builder);
        return rtt.ToString();
    }
}
namespace Solitons.Postgres.PgUp.Core.Formatting;

internal partial class PgUpScriptDisplayRtt
{
    private readonly string _relativePath;

    private PgUpScriptDisplayRtt(string relativePath)
    {
        _relativePath = relativePath;
    }

    public static string Build(string relativePath)
    {
        var rtt = new PgUpScriptDisplayRtt(relativePath);
        return rtt.ToString();
    }
}
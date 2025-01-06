namespace Solitons.Postgres.PgUp.Core.Formatting;

internal partial class PgUpTransactionDelimiterRtt
{
    /// <summary>
    /// Class to produce the template output
    /// </summary>
    private PgUpTransactionDelimiterRtt(string displayName)
    {
        DisplayName = displayName;
    }

    public string DisplayName { get; }

    public static void WriteLine(string displayName)
    {
        var rtt = new PgUpTransactionDelimiterRtt(displayName);
        Console.WriteLine((object?)rtt);
    }
}
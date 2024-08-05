using System.Diagnostics;

namespace Solitons.Postgres.PgUp;

public sealed class PgUpException : Exception
{
    internal PgUpException(string message, int exitCode = 1) : base(message)
    {
        ExitCode = exitCode;
    }

    public int ExitCode { get; }

    [DebuggerStepThrough]
    public static async Task<int> TryCatchAsync(Func<Task<int>> callback)
    {
        try
        {
            return await callback.Invoke();
        }
        catch (PgUpException e)
        {
            Console.WriteLine(e.Message);
            return e.ExitCode;
        }
    }
}
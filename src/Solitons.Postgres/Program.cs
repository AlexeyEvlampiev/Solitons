using Solitons.CommandLine;

namespace Solitons.Postgres;

internal class Program
{
    static int Main()
    {
        return CliProcessor
            .Setup(config => config
                .UseHandlersFrom<Program>())
            .Process(Environment.CommandLine);
    }


    [CliCommand("")]
    public static void Hello()
    {
        Console.WriteLine(@"Hello world!");
    }
}

using Solitons.CommandLine;

namespace Solitons.Postgres;

internal class Program
{
    static int Main()
    {
        return CliProcessor
            .Setup(config => config
                .UseCommands<Program>())
            .Process();
    }


    [CliCommand("")]
    public static void Hello()
    {
        Console.WriteLine(@"Hello world!");
    }
}

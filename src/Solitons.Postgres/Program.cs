using Solitons.CommandLine;

namespace Solitons.Postgres;

internal class Program
{
    static int Main()
    {
        return CommandLineInterface
            .Build(options => options
                .Include<Program>())
            .Execute();
    }

    [CliCommand("")]
    public static int Home()
    {
        return -1;
    }
}

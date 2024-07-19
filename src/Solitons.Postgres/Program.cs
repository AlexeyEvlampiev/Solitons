using Solitons.CommandLine;

namespace Solitons.Postgres;

internal class Program
{
    static int Main()
    {
        return CliProcessor
            .Setup(config => config
                .UseCommandHandlersFrom<Program>()
                .OnNoArguments(ShowHelp))
            .Process();
    }


    public static void ShowHelp(string helpMessage)
    {
        Console.WriteLine(Resources.Title);
        Console.WriteLine(helpMessage);
    }
}

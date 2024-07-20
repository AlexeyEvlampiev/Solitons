using System.ComponentModel;
using Solitons.CommandLine;

namespace Solitons.Postgres;

internal class Program
{
    static int Main()
    {
        return CliProcessor
            .Setup(config => config
                .UseCommands<Program>()
                .ShowAsciiHeader(Resources.Title, CliAsciiHeaderCondition.NoArguments))
            .Process();
    }

    [CliCommand("init|initialize|do-it-here")]
    [CliArgument("directory", "File directory where to initialize the new pgup project.")]
    [Description("Creates a new pgup project structure in the specified directory.")]
    public static int InitializePgUpProject(

            string directory,

            [CliOption("--template|-t", "Description goes here")]
            string projectTemplate = "basic"
        )
    {
        return 0;
    }

}

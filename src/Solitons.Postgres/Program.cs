using System.ComponentModel;
using System.Diagnostics;
using Solitons.CommandLine;
using Solitons.Postgres.PgUp;

namespace Solitons.Postgres;
using static PgUpConstants;

internal class Program
{

    static int Main()
    {
        return CliProcessor
            .Setup(config => config
                .UseCommands<Program>()
                .ShowAsciiHeader(Resources.Title, CliAsciiHeaderCondition.OnNoArguments))
            .Process();
    }

    [CliCommand(InitializeProjectCommand)]
    [CliArgument(nameof(directoryArg), ProjectDirectoryArgumentDescription)]
    [Description(InitializeProjectCommandDescription)]
    public static int InitializePgUpProject(
        PgUpProjectDirectory directoryArg,
        PgUpProjectTemplate templateOpt)
    {
        IPgUpTemplateRepository repository = new PgUpFileSystemTemplateRepository();
        DirectoryInfo di = directoryArg;
        string template = templateOpt;
        if (false == repository.Exists(template))
        {
            Console.Error.WriteLine($"Specified template not found");
            return 1;
        }

        if (false == di.Exists)
        {
            var created = ConsoleColor.Yellow.AsForegroundColor(() =>
            {
                Console.WriteLine(@"The specified directory does not exist.");
                if (CliPrompt.YesNo("Create it? [Y/N]"))
                {
                    di.Create();
                    Trace.TraceInformation($"Directory created: {di.FullName}");
                    return true;
                }

                Trace.TraceInformation($"Use disallowed");
                return false;
            });

            if (!created)
            {
                Console.WriteLine(@"Create a new empty target directory and try again.");
                return 1;
            }
        }

        Debug.Assert(di.Exists);
        if (di.GetFileSystemInfos("*").Any())
        {
            Console.Error.WriteLine($"Specified project directory is not empty.");
            return -1;
        }

        repository.Copy(template, di.FullName);

        return 0;
    }

}

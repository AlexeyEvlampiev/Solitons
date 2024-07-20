using System.ComponentModel;
using System.Diagnostics;
using Solitons.CommandLine;
using Solitons.Postgres.PgUp;

namespace Solitons.Postgres;


internal class Program
{
    public const string InitializeProjectCommand = "init|initialize";
    public const string ProjectDirectoryArgumentDescription = "File directory where to initialize the new pgup project.";
    public const string InitializeProjectCommandDescription = "Creates a new pgup project structure in the specified directory.";
    public const string TemplateParameterDescription = "The project template to be used.";

    static int Main()
    {
        return CliProcessor
            .Setup(config => config
                .UseCommands<Program>()
                .ShowAsciiHeader(Resources.Title, CliAsciiHeaderCondition.OnNoArguments))
            .Process();
    }

    [CliCommand(InitializeProjectCommand)]
    [CliArgument(nameof(directory), ProjectDirectoryArgumentDescription)]
    [Description(InitializeProjectCommandDescription)]
    public static int InitializePgUpProject(
        string directory = ".",
        [CliOption("--template|-t", TemplateParameterDescription)] string template = "basic")
    {
        IPgUpTemplateRepository repository = new PgUpFileSystemTemplateRepository();
        var di = new DirectoryInfo(directory);

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

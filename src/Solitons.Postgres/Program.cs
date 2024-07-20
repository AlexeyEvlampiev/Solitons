﻿using System.ComponentModel;
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

    private readonly IPgUpTemplateRepository _templates = new PgUpFileSystemTemplateRepository();


    static int Main()
    {
        var program = new Program();
        return CliProcessor
            .Setup(config => config
                .UseCommands(program)
                .UseAsciiHeader(Resources.Title, CliAsciiHeaderCondition.OnNoArguments))
            .Process();
    }

    [CliCommand(InitializeProjectCommand)]
    [CliArgument(nameof(directory), ProjectDirectoryArgumentDescription)]
    [Description(InitializeProjectCommandDescription)]
    public int Initialize(
        string directory = ".",
        [CliOption("--template|-t", TemplateParameterDescription)] string template = "basic")
    {
        if (false == IsValidDirectory(directory, out var di))
        {
            Console.Error.WriteLine("Invalid directory path");
            return 1;
        }

        if (false == _templates.Exists(template))
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

        _templates.Copy(template, di.FullName);


        return 0;
    }

    private bool IsValidDirectory(string directory, out DirectoryInfo info)
    {
        try
        {
            info = new DirectoryInfo(directory);
            return true;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
            info = new DirectoryInfo(".");
            return false;
        }
    }

}

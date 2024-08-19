using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Solitons.CommandLine;

internal partial class CliHelpRtt
{
    sealed record Command(string Path, string Description);

    private CliHelpRtt(string logo, string description, CliAction[] actions)
    {
        var path = Environment.GetCommandLineArgs().FirstOrDefault("tool");
        Tool = Path.GetFileName(path);
        Logo = logo;
        Description = description;
        Commands = actions
            .OrderBy(cmd => cmd)
            .Select(a => new Command(
                a.FullPath,
                a.Description))
            .Distinct()
            .ToArray();
    }

    private IEnumerable<Command> Commands { get; }
    public string Logo { get; }
    public string Description { get; }
    public string Tool { get; }

    public static string Build(string logo, string description, CliAction[] actions)
    {
        var rtt = new CliHelpRtt(logo, description, actions);
        return rtt.ToString();
    }
}
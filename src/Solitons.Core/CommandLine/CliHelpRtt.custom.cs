using System.Collections.Generic;
using System.Linq;

namespace Solitons.CommandLine;

internal partial class CliHelpRtt
{
    sealed record Command(string Path, string Description);

    private CliHelpRtt(string logo, string description, CliAction[] actions)
    {
        Logo = logo;
        Description = description;
        Commands = actions
            .OrderBy(cmd => cmd)
            .Select(a => new Command(
                a.GetHelpText(),
                a.Description))
            .Distinct()
            .ToArray();
    }

    private IEnumerable<Command> Commands { get; }
    public string Logo { get; }
    public string Description { get; }

    public static string Build(
        string logo, 
        string description, 
        CliAction[] actions)
    {
        var rtt = new CliHelpRtt(logo, description, actions);
        return rtt.ToString();
    }
}
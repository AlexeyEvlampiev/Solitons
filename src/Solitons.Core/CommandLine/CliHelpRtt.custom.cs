using System.Collections.Generic;
using System.Linq;

namespace Solitons.CommandLine;

internal partial class CliHelpRtt
{
    sealed record Command(string Path, string Description);

    private CliHelpRtt(CliActionOld[] actions)
    {
        Commands = actions
            .OrderBy(cmd => cmd)
            .Select(a => new Command(
                a.GetHelpText(),
                a.Description))
            .Distinct()
            .ToArray();
    }

    private IEnumerable<Command> Commands { get; }
    public required string Logo { get; init; }
    public required string Description { get; init; }
    public required string ProgramName { get; init; }

    public static string Build(
        string logo, 
        string programName,
        string description, 
        CliActionOld[] actions)
    {
        var rtt = new CliHelpRtt(actions)
        {
            Logo = logo,
            ProgramName = programName,
            Description = description
        };
        return rtt.ToString();
    }
}
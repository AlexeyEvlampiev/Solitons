using System.Collections.Generic;
using System.Linq;

namespace Solitons.CommandLine;

internal partial class CliHelpRtt
{
    private readonly CliAction[] _actions;

    sealed record Command(string Path, string Description);

    private CliHelpRtt(CliAction[] actions)
    {
        _actions = actions;
        Commands = _actions
            .OrderBy(_ => _)
            .Select(a => new Command(
                a.FullPath,
                a.Description))
            .Distinct()
            .ToArray();
    }

    private IEnumerable<Command> Commands { get; }

    public static string Build(CliAction[] actions)
    {
        var rtt = new CliHelpRtt(actions);
        return rtt.ToString();
    }
}
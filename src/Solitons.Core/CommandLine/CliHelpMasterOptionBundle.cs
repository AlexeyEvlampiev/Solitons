using System.Diagnostics;
using System.Reactive;

namespace Solitons.CommandLine;

internal sealed class CliHelpMasterOptionBundle : CliMasterOptionBundle
{
    [CliOption("--help|-h|-?")]
    public Unit? Help { get; set; }

    [DebuggerStepThrough]
    public override void OnExecutingAction(string commandLine)
    {
        if (Help.HasValue)
        {
            throw new CliHelpRequestedException();
        }
    }
}
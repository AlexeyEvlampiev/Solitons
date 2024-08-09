using System.Diagnostics;
using System.Reactive;

namespace Solitons.CommandLine;

internal sealed class CliHelpMasterOptionBundle : CliMasterOptionBundle
{

    [CliHelpOption]
    public Unit? Help { get; set; }

}
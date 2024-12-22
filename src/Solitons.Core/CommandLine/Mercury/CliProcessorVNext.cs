using System.Collections.Generic;

namespace Solitons.CommandLine.Mercury;

public sealed class CliProcessorVNext : CliProcessorBase
{
    protected override void OnProcessed(CliCommandLine commandLine, int exitCode)
    {
        throw new System.NotImplementedException();
    }

    protected override void OnProcessing(CliCommandLine commandLine)
    {
        throw new System.NotImplementedException();
    }

    protected override void ShowHelp(CliCommandLine commandLine)
    {
        throw new System.NotImplementedException();
    }

    protected override bool IsHelpRequest(CliCommandLine commandLine)
    {
        throw new System.NotImplementedException();
    }

    protected override IEnumerable<CliAction> GetActions()
    {
        throw new System.NotImplementedException();
    }
}
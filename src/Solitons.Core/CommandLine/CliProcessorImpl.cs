using System;
using System.Diagnostics;
using System.Linq;
using Solitons.CommandLine.ZapCli;

namespace Solitons.CommandLine;

internal sealed class CliProcessorImpl : CliProcessor
{
    private readonly CliConfigurations _configurations;


    [DebuggerNonUserCode]
    internal CliProcessorImpl(CliConfigurations configurations)
    {
        _configurations = configurations;
    }


    protected override void OnNoArguments(string message)
    {
        _configurations.OnNoArguments(message);
    }

    [DebuggerStepThrough]
    protected override IAction[] LoadActions() => _configurations
            .GetActions()
            .Cast<IAction>()
            .ToArray();


    protected override bool IsHelpRequestException(Exception ex)
    {
        return ex is ZapCliHelpRequestedException;
    }
}
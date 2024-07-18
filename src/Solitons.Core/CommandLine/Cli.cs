using System;
using System.Diagnostics;
using System.Linq;
using Solitons.CommandLine.ZapCli;

namespace Solitons.CommandLine;

internal sealed class Cli : CommandLineInterface
{
    private readonly CliConfigurations _configurations;


    [DebuggerNonUserCode]
    internal Cli(CliConfigurations configurations)
    {
        _configurations = configurations;
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
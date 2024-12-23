using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Solitons.CommandLine.Reflection;

namespace Solitons.CommandLine.Mercury;

public sealed class CliProcessorVNext : CliProcessorBase
{
    private readonly ImmutableArray<CliActionVNext> _actions;

    private CliProcessorVNext(CliActionVNext[] actions)
    {
        _actions = [.. actions];
    }

    public static CliProcessorVNext Create<T>()
    {
        var methods = CliMethodInfo
            .Get(typeof(T));

        var targets = methods
            .Where(m => m is { IsStatic: false, DeclaringType: { IsAbstract: false, IsInterface: false } })
            .Select(m => m.DeclaringType!)
            .Union([typeof(T)])
            .Distinct()
            .Select(Activator.CreateInstance)
            .SkipNulls()
            .ToList();

        var actions = methods
            .Select(m =>
            {
                if (m.IsStatic)
                {
                    return new CliActionVNext(m, null);
                }

                var target = targets.Single(t => m.DeclaringType!.IsInstanceOfType(t));
                return new CliActionVNext(m, target);
            })
            .ToArray();
        return new CliProcessorVNext(actions);
    }

    [DebuggerStepThrough]
    public static int Process<T>(string commandLine)
    {
        return Create<T>().Process(commandLine);
    }

    protected override bool IsActionHelpRequest(CliCommandLine commandLine)
    {
        throw new System.NotImplementedException();
    }

    protected override void OnProcessed(CliCommandLine commandLine, int exitCode)
    {
        throw new System.NotImplementedException();
    }

    protected override void OnProcessing(CliCommandLine commandLine)
    {
        throw new System.NotImplementedException();
    }

    protected override void ShowGeneralHelp(CliCommandLine commandLine)
    {
        throw new System.NotImplementedException();
    }



    protected override IEnumerable<CliAction> GetActions() => _actions;

    sealed class CliActionVNext(CliMethodInfo method, object? instance) : CliAction
    {
        private readonly object? _instance = instance;

        [DebuggerStepThrough]
        public override bool IsMatch(CliCommandLine commandLine) => method.IsMatch(commandLine);

        [DebuggerStepThrough]
        public override double Rank(CliCommandLine commandLine) => method.Rank(commandLine);

        public override int Process(CliCommandLine commandLine)
        {
            throw new System.NotImplementedException();
        }

        public override void ShowHelp(CliCommandLine commandLine)
        {
            throw new System.NotImplementedException();
        }

        public override string ToString() => method.ToString();
    }
}
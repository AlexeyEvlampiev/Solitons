using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Solitons.Collections;
using Solitons.CommandLine.Common;
using Solitons.CommandLine.Reflection;

namespace Solitons.CommandLine;

public class CliProcessor : CliProcessorBase
{
    private readonly ImmutableArray<CliActionVNext> _actions;
    private readonly ImmutableArray<CliGlobalOptionBundle> _globalOptions;

    protected interface ICliProcessorConfig
    {
        FluentList<CliGlobalOptionBundle> GlobalOptions { get; }
    }

    sealed class Config : ICliProcessorConfig
    {
        public FluentList<CliGlobalOptionBundle> GlobalOptions { get; } = new();
    }

    private CliProcessor(CliActionVNext[] actions)
    {
        _actions = [.. actions];
    }

    protected CliProcessor(Action<ICliProcessorConfig> initialize)
    {
        var config = new Config();
        initialize.Invoke(config);
        _globalOptions = [.. config.GlobalOptions.Distinct()];
        _actions = [.. GetActions(GetType(), this)];
    }

    [DebuggerStepThrough]
    public static CliProcessor From<T>() => From(typeof(T));


    public static CliProcessor From(Type type)
    {
        if (type.IsInterface ||
            type.IsAbstract)
        {
            throw new ArgumentException("Oops...");
        }

        var instance = Activator.CreateInstance(type);
        return From(type, instance);
    }

    [DebuggerStepThrough]
    public static CliProcessor From(object program) => From(program.GetType(), program);


    private static CliProcessor From(Type type, object? instance)
    {
        var actions = GetActions(type, instance);
        return new CliProcessor(actions);
    }

    [DebuggerStepThrough]
    public static int Process<T>(string commandLine)
    {
        return From<T>().Process(commandLine);
    }


    private static CliActionVNext[] GetActions(Type type, object? instance)
    {
        var methods = CliMethodInfo
            .Get(type);

        var actions = methods
            .Select(m =>
            {
                if (m.IsStatic)
                {
                    return new CliActionVNext(m, null);
                }

                ThrowIf.NullReference(instance);
                return new CliActionVNext(m, instance);
            })
            .ToArray();
        return actions;
    }







    protected override void DisplayGeneralHelp(CliCommandLine commandLine)
    {
        if (Logo.IsPrintable())
        {
            Console.WriteLine(Logo);
            Enumerable.Range(0, 1).ForEach(_ => Console.WriteLine());
        }

        if (Description.IsPrintable())
        {
            Console.WriteLine(Description);
            Enumerable.Range(0, 2).ForEach(_ => Console.WriteLine());
        }

        foreach (var action in _actions)
        {
            var actionHelp = action.GetGeneralHelp();
            Console.WriteLine(actionHelp);
            Enumerable.Range(0, 2).ForEach(_ => Console.WriteLine());
        }
    }



    protected override IEnumerable<CliAction> GetActions() => _actions;

    sealed class CliActionVNext(CliMethodInfo method, object? instance) : CliAction
    {
        private readonly object? _instance = instance;

        [DebuggerStepThrough]
        public override bool IsMatch(CliCommandLine commandLine) => method.IsMatch(commandLine);

        [DebuggerStepThrough]
        public override double Rank(CliCommandLine commandLine) => method.Rank(commandLine);

        [DebuggerStepThrough]
        public override int Process(CliCommandLine commandLine) => method.Invoke(instance, commandLine);

        public override void ShowHelp(CliCommandLine commandLine)
        {
            throw new NotImplementedException();
        }

        [DebuggerStepThrough]
        public override double RankByOptions(CliCommandLine commandLine) => method.RankByOptions(commandLine);

        public override string ToString() => method.ToString();

        [DebuggerStepThrough]
        public string GetGeneralHelp() => method.ToGeneralHelpString();
    }

    protected override void OnExecutingAction(CliCommandLine commandLine)
    {
        foreach (var bundle in _globalOptions)
        {
            bundle.OnExecutingAction(commandLine);
        }
    }

    protected override void OnActionExecuted(CliCommandLine commandLine, int exitCode)
    {
        foreach (var bundle in _globalOptions)
        {
            bundle.OnActionExecuted(commandLine);
        }
    }
}
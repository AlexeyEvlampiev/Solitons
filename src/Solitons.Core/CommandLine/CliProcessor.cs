using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Solitons.CommandLine.Common;
using Solitons.CommandLine.Reflection;

namespace Solitons.CommandLine;

public sealed class CliProcessor : CliProcessorBase
{
    private readonly ImmutableArray<CliActionVNext> _actions;

    private CliProcessor(CliActionVNext[] actions)
    {
        _actions = [.. actions];
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
        return new CliProcessor(actions);
    }




    [DebuggerStepThrough]
    public static int Process<T>(string commandLine)
    {
        return From<T>().Process(commandLine);
    }




    protected override void ShowGeneralHelp(CliCommandLine commandLine)
    {
        throw new NotImplementedException();
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
    }
}
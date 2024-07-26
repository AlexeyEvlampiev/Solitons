using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Solitons.CommandLine;

public sealed class CliProcessor
{
    private sealed record Source(
        Type DeclaringType,
        object? Instance,
        CliCommandAttribute[] RootCommands,
        BindingFlags BindingFlags);

    private sealed record AsciiHeaderConfig(string Text, CliAsciiHeaderCondition When);

    private readonly List<Source> _sources = new();
    private readonly CliAction[] _actions;
    private AsciiHeaderConfig _headerConfig = new("", CliAsciiHeaderCondition.Always);



    private CliProcessor(Action<IOptions> config)
    {
        var masterOptionBundles = new CliMasterOptionBundle[]
        {
            new CliHelpMasterOptionBundle(),
            new CliTraceMasterOptionsBundle()
        };


        var options = new Options(this);
        config.Invoke(options);
        var actions = new List<CliAction>();
        foreach (var source in _sources)
        {
            foreach (var mi in source.DeclaringType.GetMethods(source.BindingFlags))
            {
                if (false == mi
                        .GetCustomAttributes()
                        .OfType<CliCommandAttribute>()
                        .Any())
                {
                    Debug.WriteLine($"Not an action: {mi.Name}");
                    continue;
                }

                actions.Add(new CliAction(source.Instance, mi, masterOptionBundles));
            }
        }

        _actions = actions.ToArray();
    }

    public interface IOptions
    {
        [DebuggerStepThrough]
        public sealed IOptions UseCommands<T>(BindingFlags binding = BindingFlags.Static | BindingFlags.Public) => 
            UseCommands(typeof(T), binding);

        [DebuggerStepThrough]
        public sealed IOptions UseCommands(Type declaringType,
            BindingFlags binding = BindingFlags.Static | BindingFlags.Public) =>
            UseCommands(declaringType, [], binding);

        [DebuggerStepThrough]
        public sealed IOptions UseCommands(object target,
            BindingFlags binding = BindingFlags.Instance | BindingFlags.Public) =>
            UseCommands(target, [], binding);

        IOptions UseCommands(Type declaringType, CliCommandAttribute[] rootCommands, BindingFlags binding = BindingFlags.Static | BindingFlags.Public);
        IOptions UseCommands(object target, CliCommandAttribute[] rootCommands, BindingFlags binding = BindingFlags.Instance | BindingFlags.Public);
        IOptions UseAsciiHeader(string asciiHeaderText, CliAsciiHeaderCondition condition);
    }


    sealed class Options : IOptions
    {
        private readonly CliProcessor _processor;

        public Options(CliProcessor processor)
        {
            _processor = processor;
        }

        [DebuggerStepThrough]
        public IOptions UseCommands(
            object target,
            CliCommandAttribute[] rootCommands,
            BindingFlags binding)
        {
            return UseCommands(target, target.GetType(), rootCommands, binding);
        }

        [DebuggerStepThrough]
        public IOptions UseCommands(
            Type declaringType,
            CliCommandAttribute[] rootCommands,
            BindingFlags binding)
        {
            return UseCommands(null, declaringType, rootCommands, binding);
        }

        [DebuggerStepThrough]
        private IOptions UseCommands(
            object? instance,
            Type declaringType, 
            CliCommandAttribute[] rootCommands, 
            BindingFlags binding)
        {
            if (binding.HasFlag(BindingFlags.Instance))
            {
                instance ??= Activator.CreateInstance(declaringType);
                if (instance == null)
                {
                    throw new NotImplementedException();
                }
            }
            _processor._sources.Add(new Source(declaringType, instance, rootCommands, binding));
            return this;
        }

        public IOptions UseAsciiHeader(string text, CliAsciiHeaderCondition condition)
        {
            _processor._headerConfig = new AsciiHeaderConfig(text, condition);
            return this;
        }
    }

    [DebuggerStepThrough]
    public static CliProcessor Setup(Action<IOptions> config) => new(config);

    [DebuggerStepThrough]
    public int Process() => Process(Environment.CommandLine);

    public int Process(string commandLine)
    {
        commandLine = commandLine.Trim();
        
        try
        {
            var selectedActions = _actions
                .Where(a => a.IsMatch(commandLine))
                .GroupBy(a => a.CalcSimilarity(commandLine))
                .OrderByDescending(similarMatchedActions => similarMatchedActions.Key)
                .Take(1)
                .SelectMany(similarMatchedActions => similarMatchedActions)
                .ToList();

            if (selectedActions.Count != 1)
            {
                Trace.TraceInformation($"Found {selectedActions.Count} actions that matched the given command line.");

                if (_headerConfig.Text.IsPrintable() && 
                    CliTokenSubstitutionPreprocessor.Parse(commandLine).Count() == 1)
                {
                    Console.WriteLine(_headerConfig.Text);
                }
                ShowHelp(commandLine);
                return 1;
            }

            var action = selectedActions[0];

            Trace.TraceInformation($"Found an actions that matches the given command line.");

            var result = action.Execute(commandLine);
            Trace.TraceInformation($"The action returned {result}");

            return result;
        }
        catch (CliHelpRequestedException e) 
        {
            ShowHelp(commandLine);
            return 1;
        }
        catch (Exception e)
        {
            Trace.TraceError(e.ToString());
            Console.WriteLine("Internal error");
            return 1;
        }

    }



    public abstract class Similarity : IComparable<Similarity>
    {
        protected abstract int CompareTo(Similarity other);

        [DebuggerStepThrough]
        int IComparable<Similarity>.CompareTo(Similarity? other)
        {
            if (other?.GetType() != this.GetType())
            {
                throw new ArgumentOutOfRangeException();
            }

            return CompareTo(other);
        }
    }
    



    public void ShowHelp(string commandLine)
    {
        var matchesFound = _actions.Any(a => a.IsMatch(commandLine));
        _actions
            .Where(a => false == matchesFound || a.IsMatch(commandLine))
            .GroupBy(a => a.CalcSimilarity(commandLine))
            .OrderByDescending(similarActions => similarActions.Key)
            .Take(1)
            .SelectMany(similarActions => similarActions)
            .ForEach((action) =>action.ShowHelp());
    }
}
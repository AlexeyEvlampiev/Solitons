using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

public sealed class CliProcessor : ICliProcessorCallback
{
    private sealed record Source(
        Type DeclaringType,
        object? Instance,
        CliCommandAttribute[] RootCommands,
        BindingFlags BindingFlags);

    private readonly List<Source> _sources = new();
    private readonly CliAction[] _actions;
    private ICliProcessorCallback _callback;
    private string _logo = string.Empty;
    private string _description = string.Empty;


    private CliProcessor(
        Action<IOptions> config,
        ICliProcessorCallback? callback = null)
    {
        _callback = callback ?? this;
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
        public sealed IOptions UseCommandsFrom<T>(BindingFlags binding = BindingFlags.Static | BindingFlags.Public) => 
            UseCommandsFrom(typeof(T), binding);

        [DebuggerStepThrough]
        public sealed IOptions UseCommandsFrom(Type declaringType,
            BindingFlags binding = BindingFlags.Static | BindingFlags.Public) =>
            UseCommandsFrom(declaringType, [], binding);

        [DebuggerStepThrough]
        public sealed IOptions UseCommandsFrom(object target,
            BindingFlags binding = BindingFlags.Instance | BindingFlags.Public) =>
            UseCommandsFrom(target, [], binding);


        IOptions UseCommandsFrom(Type declaringType, CliCommandAttribute[] rootCommands, BindingFlags binding = BindingFlags.Static | BindingFlags.Public);
        IOptions UseCommandsFrom(object target, CliCommandAttribute[] rootCommands, BindingFlags binding = BindingFlags.Instance | BindingFlags.Public);
        IOptions UseLogo(string logo);
        IOptions UseDescription(string description);
        internal IOptions UseCallback(ICliProcessorCallback callback);
    }


    sealed class Options : IOptions
    {
        private readonly CliProcessor _processor;

        public Options(CliProcessor processor)
        {
            _processor = processor;
        }

        [DebuggerStepThrough]
        public IOptions UseCommandsFrom(
            object target,
            CliCommandAttribute[] rootCommands,
            BindingFlags binding)
        {
            return UseCommands(target, target.GetType(), rootCommands, binding);
        }

        [DebuggerStepThrough]
        public IOptions UseCommandsFrom(
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

        public IOptions UseLogo(string logo)
        {
            _processor._logo = logo;
            return this;
        }

        public IOptions UseDescription(string description)
        {
            _processor._description = description
                .DefaultIfNullOrWhiteSpace(String.Empty)
                .Trim();
            return this;
        }

        public IOptions UseCallback(ICliProcessorCallback callback)
        {
            _processor._callback = callback;
            return this;
        }
    }

    [DebuggerStepThrough]
    public static CliProcessor Setup(Action<IOptions> config) => new(config);

    [DebuggerStepThrough]
    public int Process() => Process(Environment.CommandLine);

    public int Process(string commandLine)
    {
        try
        {
            commandLine = CliTokenSubstitutionPreprocessor
                .SubstituteTokens(commandLine, out var preProcessor)
                .Trim();
            var executableName = Regex
                .Match(commandLine, @"^\S+")
                .Convert( m => preProcessor.GetSubstitution(m.Value))
                .Convert(Path.GetFileName)
                .DefaultIfNullOrWhiteSpace("cli");

            if (CliHelpOptionAttribute.IsMatch(commandLine))
            {
                _callback.ShowHelp(executableName, commandLine);
                return 0;
            }

            var selectedActions = _actions
            .Where(a => a.IsMatch(commandLine))
            .GroupBy(a => a.Rank(commandLine))
            .OrderByDescending(group => group.Key)
            .Do(group => Trace.WriteLine(group.Count()))
            .Take(1)
            .SelectMany(similarMatchedActions => similarMatchedActions)
            .ToList();

            if (selectedActions.Count != 1)
            {
                Console.WriteLine("No matching commands found. See closest commands description below. ");
                _callback.ShowHelp(executableName, commandLine);
                return 1;
            }

            var action = selectedActions[0];

            Trace.TraceInformation($"Found an actions that matches the given command line.");

            var result = action.Execute(commandLine, preProcessor);
            Trace.TraceInformation($"The action returned {result}");

            return result;
        }
        catch (CliExitException e)
        {
            Console.Error.WriteLine(e.Message);
            return e.ExitCode;
        }
        catch (Exception e)
        {
            Trace.TraceError(e.ToString());
            Console.WriteLine("Internal error");
            return 1;
        }

    }

    void ICliProcessorCallback.ShowHelp(
        string executableName, 
        string commandLine)
    {
        var anyMatchesFound = _actions.Any(a => a.IsMatch(commandLine));
        if (false == anyMatchesFound &&
            CliHelpOptionAttribute.IsRootHelpCommand(commandLine))
        {
            var help = CliHelpRtt.Build(
                executableName, 
                _logo, 
                _description, 
                _actions);
            Console.WriteLine(help);
            return;
        }

        var similarGroups = _actions
            .Where(a => false == anyMatchesFound || a.IsMatch(commandLine))
            .GroupBy(a => a.Rank(commandLine))
            .ToList();

        var text = _actions
            .Where(a => false == anyMatchesFound || a.IsMatch(commandLine))
            .GroupBy(a => a.Rank(commandLine))
            .OrderByDescending(similarActions => similarActions.Key)
            .Take(1)
            .SelectMany(similarActions => similarActions)
            .Convert(selected => CliActionHelpRtt.Build(executableName, selected));
        Console.WriteLine(text);
    }

}
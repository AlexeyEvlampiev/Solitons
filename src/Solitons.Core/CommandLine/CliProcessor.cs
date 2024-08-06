﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

public sealed class CliProcessor
{
    private sealed record Source(
        Type DeclaringType,
        object? Instance,
        CliCommandAttribute[] RootCommands,
        BindingFlags BindingFlags);

    private readonly List<Source> _sources = new();
    private readonly CliAction[] _actions;
    private string _logo = String.Empty;



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

        var showHelpMi = GetType()
            .GetMethod(
                nameof(ShowHelp), BindingFlags.Instance | BindingFlags.NonPublic, 
                [typeof(CliHelpMasterOptionBundle)]) ?? throw new InvalidOperationException();
        actions.Add(new CliAction(this, showHelpMi, []));
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
        IOptions UseLogo(string asciiHeaderText);
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

        public IOptions UseLogo(string text)
        {
            _processor._logo = text;
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
        commandLine = CliTokenSubstitutionPreprocessor.SubstituteTokens(commandLine, out var preProcessor);

        try
        {
            var selectedActions = _actions
                .Where(a => a.IsMatch(commandLine))
                .GroupBy(a => a.CalcSimilarity(commandLine))
                .OrderByDescending(similarMatchedActions => similarMatchedActions.Key)
                .Do(g => Trace.WriteLine(g.Count()))
                .Take(1)
                .SelectMany(similarMatchedActions => similarMatchedActions)
                .ToList();

            if (selectedActions.Count != 1)
            {
                Trace.TraceInformation($"Found {selectedActions.Count} actions that matched the given command line.");

                ShowHelp(commandLine);
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

    [CliCommand("")]
    private void ShowHelp(CliHelpMasterOptionBundle help)
    {
        if (_logo.IsPrintable())
        {
            Console.WriteLine(_logo);
        }

        var text = CliHelpRtt.Build(_actions);
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
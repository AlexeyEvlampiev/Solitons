using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Solitons.Caching;

namespace Solitons.CommandLine;

internal sealed class CliProcessor : 
    ICliProcessor.Options, 
    ICliProcessor
{
    private sealed record Source(
        Type DeclaringType,
        object? Instance,
        string BaseRoute,
        BindingFlags BindingFlags);

    private readonly List<Source> _sources = new();
    private readonly CliAction[] _actions;
    private readonly CliRouteAttribute[] _baseRouteMetadata = [];
    private string _logo = string.Empty;
    private string _description = string.Empty;
    private readonly IInMemoryCache _cache = IInMemoryCache.Create();


    public CliProcessor()
    {
        var masterOptionBundles = new CliMasterOptionBundle[]
        {
            new CliHelpMasterOptionBundle(),
            new CliTraceMasterOptionsBundle()
        };

        var actions = new List<CliAction>();

        foreach (var source in _sources)
        {
            var methods = source
                .DeclaringType
                .Convert(type => type.GetInterfaces().Concat([type]))
                .Distinct()
                .SelectMany(type => type.GetMethods(source.BindingFlags))
                .Distinct();

            foreach (var mi in methods)
            {
                if (false == mi
                        .GetCustomAttributes()
                        .OfType<CliRouteAttribute>()
                        .Any())
                {
                    Debug.WriteLine($"Not an action: {mi.Name}");
                    continue;
                }

                Debug.WriteLine($"Action: {mi.Name}");
                actions.Add(CliAction.Create(
                    source.Instance, 
                    mi, 
                    masterOptionBundles, 
                    _baseRouteMetadata,
                    _cache));
            }
        }

        _actions = actions.ToArray();
    }


    public void OnMultipleMatchesFound()
    {
        throw new NotImplementedException();
    }

    public void OnNoMatch()
    {
        throw new NotImplementedException();
    }

    ICliAction[] ICliProcessor.GetActions()
    {
        throw new NotImplementedException();
    }

    public void ShowHelpFor(string commandLine, CliTokenDecoder decoder)
    {
        var programName = Regex
            .Match(commandLine, @"^\s*\S+")
            .Convert(m => decoder(m.Value))
            .DefaultIfNullOrWhiteSpace("program")
            .Convert(Path.GetFileName)
            .DefaultIfNullOrWhiteSpace("program");


        if (CliHelpOptionAttribute.IsGeneralHelpRequest(commandLine))
        {
            var help = CliHelpRtt.Build(
                _logo,
                programName,
                _description,
                _actions);
            Console.WriteLine(help);
            return;
        }

        var noMatchedActions = (false == _actions.Any(a => a.IsMatch(commandLine)));
        var text = _actions
            .Where(a => noMatchedActions || a.IsMatch(commandLine))
            .GroupBy(a => a.Rank(commandLine))
            .OrderByDescending(similarActions => similarActions.Key)
            .Take(1)
            .SelectMany(similarActions => similarActions)
            .Convert(CliActionHelpRtt.ToString);
        Console.WriteLine(text);
    }

    public void ShowGeneralHelp(string programName)
    {
        var help = CliHelpRtt.Build(
            _logo,
            programName,
            _description,
            _actions);
        Console.WriteLine(help);
    }

    public bool IsGeneralHelpRequest(string commandLine)
    {
        throw new NotImplementedException();
    }

    public bool IsSpecificHelpRequest(string commandLine)
    {
        throw new NotImplementedException();
    }

    public string GetFileName(string filePath)
    {
        throw new NotImplementedException();
    }


    [DebuggerStepThrough]
    public override ICliProcessor.Options UseCommandsFrom(
        object program, 
        string baseRoute = "", 
        BindingFlags binding = BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
    {
        return UseCommands(program, program.GetType(), baseRoute, binding);
    }

    [DebuggerStepThrough]
    public override ICliProcessor.Options UseCommandsFrom(
        Type declaringType, 
        string baseRoute = "",
        BindingFlags binding = BindingFlags.Default | BindingFlags.Static | BindingFlags.Public)
    {
        return UseCommands(null, declaringType, baseRoute, binding);
    }

    public override ICliProcessor.Options UseLogo(string logo)
    {
        _logo = logo;
        return this;
    }

    public override ICliProcessor.Options UseDescription(string description)
    {
        _description = description;
        return this;
    }


    [DebuggerStepThrough]
    private CliProcessor UseCommands(
        object? instance,
        Type declaringType,
        string baseRoute,
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
        _sources.Add(new Source(declaringType, instance, baseRoute, binding));
        return this;
    }
}
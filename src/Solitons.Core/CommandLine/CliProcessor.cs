using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Solitons.Caching;

namespace Solitons.CommandLine;

internal sealed class CliProcessor : ICliProcessor
{
    private sealed record Source(
        Type DeclaringType,
        object? Instance,
        string BaseRoute,
        BindingFlags BindingFlags);

    private sealed record HelpCommandData(string Aliases, string Description);

    private readonly List<Source> _sources = new();
    private readonly CliAction[] _actions;
    private readonly string _baseRoute;
    private string _logo = string.Empty;
    private string _description = string.Empty;
    private string _baseRoot = string.Empty;
    private HelpCommandData? _helpCommandData = null;
    private readonly IMemoryCache _cache = IMemoryCache.Create();


    public CliProcessor(Action<ICliConfigOptions> config)
    {
        config.Invoke(new Options(this));

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
                    _baseRoot,
                    [],
                    _cache));
            }
        }

        if (_helpCommandData is not null)
        {
            var mi = GetType().GetMethod(nameof(CliProcessor.ShowCommandList));
            mi = ThrowIf.NullReference(mi);
            var attributes = new Attribute[]
            {
                new DescriptionAttribute(_helpCommandData.Description)
            };
            var action = CliAction.Create(this, mi, masterOptionBundles, "", attributes, _cache);
            actions.Add(action);
        }
        _actions = actions.ToArray();
    }


    public void OnMultipleMatchesFound()
    {
        Console.WriteLine("Multiple matching commands found. Please refine your input to specify a single command.");
    }

    public void OnNoMatch()
    {
        Console.WriteLine("Command not recognized. Use '--help' to view available commands.");
    }


    ICliAction[] ICliProcessor.GetActions() => _actions.ToArray();

    public void ShowCommandHelp(string commandLine, CliTokenDecoder decoder)
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

    public void ShowCommandList(string programName)
    {
        var help = CliGeneralHelpRtt
            .Build(
                _logo, 
                programName, 
                _description, 
                _actions
                    .Select(a => a.GetSchema())
                    .ToArray());
        Console.WriteLine(help);
    }




    sealed class Options(CliProcessor processor) : ICliConfigOptions
    {
        [DebuggerStepThrough]
        public ICliConfigOptions UseCommandsFrom(
            object program,
            string baseRoute = "",
            BindingFlags binding = BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        {
            return UseCommands(program, program.GetType(), baseRoute, binding);
        }

        [DebuggerStepThrough]
        public ICliConfigOptions UseCommandsFrom(
            Type declaringType,
            string baseRoute = "",
            BindingFlags binding = BindingFlags.Default | BindingFlags.Static | BindingFlags.Public)
        {
            return UseCommands(null, declaringType, baseRoute, binding);
        }

        public ICliConfigOptions UseLogo(string logo)
        {
            processor._logo = logo;
            return this;
        }

        public ICliConfigOptions UseDescription(string description)
        {
            processor._description = description;
            return this;
        }

        public ICliConfigOptions AddHelpCommand(
            CliRouteAttribute route, 
            DescriptionAttribute description)
        {
            processor._helpCommandData = new HelpCommandData(route.PsvExpression, description.Description);
            return this;
        }


        [DebuggerStepThrough]
        private ICliConfigOptions UseCommands(
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
            processor._sources.Add(new Source(declaringType, instance, baseRoute, binding));
            return this;
        }
    }

}
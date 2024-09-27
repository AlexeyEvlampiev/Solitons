using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Solitons.Caching;
using Solitons.CommandLine.Models;
using Solitons.CommandLine.Models.Formatters;

namespace Solitons.CommandLine;

internal sealed class CliProcessor : ICliProcessor
{

    private sealed record HelpCommandData(string Aliases, string Description);

    private readonly List<CliModule> _modules = new();
    private readonly CliAction[] _actions;
    private string _logo = string.Empty;
    private string _description = string.Empty;
    private string _baseRoute = string.Empty;
    private HelpCommandData? _helpCommandData = null;
    private readonly IMemoryCache _cache = IMemoryCache.Create();
    private readonly CliModel _model;


    public CliProcessor(Action<ICliConfigOptions> config)
    {
        config.Invoke(new Options(this));

        _model = new CliModel(
            _modules,
            new CliMasterOptionBundle[]
            {
                new CliHelpMasterOptionBundle(),
                new CliTraceMasterOptionsBundle()
            },
            _description,
            _logo,
            _baseRoute
        );

        var masterOptionBundles = new CliMasterOptionBundle[]
        {
            new CliHelpMasterOptionBundle(),
            new CliTraceMasterOptionsBundle()
        };

        var actions = new List<CliAction>();

        foreach (var source in _modules)
        {
            var methods = source
                .ProgramType
                .Convert(type => type.GetInterfaces().Concat([type]))
                .Distinct()
                .SelectMany(type => type.GetMethods(source.Binding))
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
                    source.Program, 
                    mi, 
                    masterOptionBundles,
                    _baseRoute,
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
                _model);
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
            processor._modules.Add(new CliModule(program, binding, baseRoute));
            return this;
        }

        [DebuggerStepThrough]
        public ICliConfigOptions UseCommandsFrom(
            Type declaringType,
            string baseRoute = "",
            BindingFlags binding = BindingFlags.Default | BindingFlags.Static | BindingFlags.Public)
        {
            processor._modules.Add(new CliModule(declaringType, binding, baseRoute));
            return this;
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
            processor._helpCommandData = new HelpCommandData(route.RouteDeclaration, description.Description);
            return this;
        }


    }

}
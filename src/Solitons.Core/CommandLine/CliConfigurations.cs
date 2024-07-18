using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Solitons.CommandLine.ZapCli;

namespace Solitons.CommandLine;

internal sealed class CliConfigurations : CommandLineInterface.IOptions
{
    private IServiceProvider _serviceProvider = new ActivatorServiceProvider();
    private ActivationSource _activationSource;

    sealed record ActivationSource(
        Type ActivationSourceType,
        object? ActionSource,
        BindingFlags BindingFlags);

    sealed record Source(
        Type ActionSourceType,
        object? ActionSource,
        CliCommandAttribute[] RootCommands,
        BindingFlags BindingFlags);



    private readonly List<Source> _sources = new();
    private readonly List<CliAction> _actions = new();

    [DebuggerNonUserCode]
    internal CliConfigurations()
    {
        _activationSource = new ActivationSource(GetType(), this, BindingFlags.Static);
    }




    public List<CliCommandAttribute> RootCommands { get; private set; } = new();


    public CommandLineInterface.IOptions AddHandler(
        Type type,
        CliCommandAttribute[] rootCommands,
        BindingFlags binding = BindingFlags.Static | BindingFlags.Public)
    {
        object? instance = null;
        if (binding.HasFlag(BindingFlags.Instance))
        {
            instance = Activator.CreateInstance(type);
            if (instance == null)
            {
                throw new NotImplementedException();
            }
        }


        return AddHandler(type, instance, rootCommands, binding);
    }

    [DebuggerStepThrough]
    public CommandLineInterface.IOptions Include<T>(BindingFlags binding)
    {
        return AddHandler(typeof(T), Array.Empty<CliCommandAttribute>(), binding);
    }

    [DebuggerStepThrough]
    public CommandLineInterface.IOptions AddHandler<T>(
        CliCommandAttribute[] rootCommands,
        BindingFlags binding = BindingFlags.Static | BindingFlags.Public)
    {
        return AddHandler(typeof(T), rootCommands, binding);
    }


    [DebuggerStepThrough]
    public CommandLineInterface.IOptions AddHandler(
        Type type,
        BindingFlags binding = BindingFlags.Static | BindingFlags.Public)
    {
        return AddHandler(type, Array.Empty<CliCommandAttribute>(), binding);
    }


    [DebuggerStepThrough]
    public CommandLineInterface.IOptions AddHandler(
        object instance,
        CliCommandAttribute[] rootCommands,
        BindingFlags binding = BindingFlags.Instance | BindingFlags.Public)
    {
        return AddHandler(instance.GetType(), instance, rootCommands, binding);
    }

    internal CliAction[] ToActions() => _actions.ToArray();

    private CliConfigurations AddHandler(
        Type type,
        object? instance,
        CliCommandAttribute[] rootCommands,
        BindingFlags binding)
    {
        _sources.Add(new Source(type, instance, rootCommands, binding));


        return this;
    }




    public IEnumerable<CliAction> GetActions()
    {
        var masterOptions = new CliMasterOptionBundle[]
        {
            new CliHelpOption(),
            new CliTraceMasterOptionsBundle()
        };

        foreach (var source in _sources)
        {
            foreach (var method in source.ActionSourceType.GetMethods(source.BindingFlags))
            {
                if (false == CliAction.IsAction(method))
                {
                    continue;
                }

                yield return new CliAction(source.ActionSource, method, masterOptions);
            }
        }
    }

}
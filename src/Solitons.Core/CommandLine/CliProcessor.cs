using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Solitons.Collections;
using Solitons.CommandLine.Common;
using Solitons.CommandLine.Reflection;

namespace Solitons.CommandLine;

public sealed class CliProcessor : CliProcessorBase
{
    private readonly string _logo;
    private readonly string _description;
    private readonly ImmutableArray<CliAction> _actions;

    sealed record Service(Type ServiceType, object? Instances, ImmutableArray<CliRouteAttribute> RootRoutes);
    sealed class Config : ICliProcessorConfig
    {
        public FluentList<CliGlobalOptionBundle> GlobalOptions { get; } = new();

        public List<Service> Services { get; } = new();

        public string Logo { get; set; }

        public string Description { get; set; }

        public ICliProcessorConfig WithLogo(string logo)
        {
            Logo = logo;
            return this;
        }


        public ICliProcessorConfig WithDescription(string description)
        {
            Description = description;
            return this;
        }

        public ICliProcessorConfig ConfigGlobalOptions(Action<FluentList<CliGlobalOptionBundle>> config)
        {
            config.Invoke(GlobalOptions);
            return this;
        }

        public ICliProcessorConfig AddService(
            object instance, 
            IEnumerable<CliRouteAttribute> rootRoutes)
        {
            Services.Add(new Service(instance.GetType(), instance, [..rootRoutes.Distinct()]));
            return this;
        }

        public ICliProcessorConfig AddService(Type serviceType, IEnumerable<CliRouteAttribute> rootRoutes)
        {
            var ctor = serviceType.GetConstructor([]);
            object? instance = null;
            if (serviceType.IsAbstract || ctor is null)
            {
                instance = null;
            }
            else
            {
                instance = ctor.Invoke([]);
            }

            Services.Add(new Service(serviceType, instance, [.. rootRoutes.Distinct()]));

            return this;
        }
    }


    private CliProcessor(Action<ICliProcessorConfig> initialize)
    {
        var config = new Config();
        initialize.Invoke(config);
        _logo = config.Logo;
        _description = config.Description;
        
        var globalOptions = config.GlobalOptions.Distinct();
        _actions = 
            [
                ..config.Services
                    .SelectMany(service =>
                    {
                        var context = new CliContext(service.RootRoutes, globalOptions);
                        return CliMethodInfo
                            .Get(service.ServiceType, context)
                            .Select(method => new CliAction(method, service.Instances));
                    })
            ];
    }


    [DebuggerStepThrough]
    public static CliProcessor Create(Action<ICliProcessorConfig> initialize)
    {
        return new CliProcessor(initialize);
    }



    private static CliAction[] GetActions(Type type, object? instance, CliContext context)
    {
        var methods = CliMethodInfo
            .Get(type, context);

        var actions = methods
            .Select(m =>
            {
                if (m.IsStatic)
                {
                    return new CliAction(m, null);
                }

                ThrowIf.NullReference(instance);
                return new CliAction(m, instance);
            })
            .ToArray();
        return actions;
    }







    protected override void DisplayGeneralHelp(CliCommandLine commandLine)
    {
        if (_logo.IsPrintable())
        {
            Console.WriteLine(_logo);
            Enumerable.Range(0, 1).ForEach(_ => Console.WriteLine());
        }

        if (_description.IsPrintable())
        {
            Console.WriteLine(_description);
            Enumerable.Range(0, 2).ForEach(_ => Console.WriteLine());
        }

        foreach (var action in _actions)
        {
            var actionHelp = action.GetGeneralHelp();
            Console.WriteLine(actionHelp);
            Enumerable.Range(0, 2).ForEach(_ => Console.WriteLine());
        }
    }



    protected override IEnumerable<IAction> GetActions() => _actions;

    sealed class CliAction(CliMethodInfo method, object? instance) : IAction
    {
        [DebuggerStepThrough]
        bool IAction.IsMatch(CliCommandLine commandLine) => method.IsMatch(commandLine);


        [DebuggerStepThrough]
        int IAction.Process(CliCommandLine commandLine) => method.Invoke(instance, commandLine);

        void IAction.ShowHelp(CliCommandLine commandLine)
        {
            throw new NotImplementedException();
        }

        [DebuggerStepThrough]
        double IAction.RankByOptions(CliCommandLine commandLine) => method.RankByOptions(commandLine);

        public override string ToString() => method.ToString();

        [DebuggerStepThrough]
        public string GetGeneralHelp() => method.ToGeneralHelpString();
    }
}
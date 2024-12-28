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

    public interface ICliProcessorConfig
    {
        ICliProcessorConfig WithProcessorAsService(bool processorIsService);
        ICliProcessorConfig ConfigGlobalOptions(Action<FluentList<CliGlobalOptionBundle>> config);
        ICliProcessorConfig AddService(object instance, IEnumerable<CliRouteAttribute> rootRoutes);
        ICliProcessorConfig AddService(Type serviceType, IEnumerable<CliRouteAttribute> rootRoutes);

        [DebuggerStepThrough]
        public sealed ICliProcessorConfig AddService(object instance) => AddService(instance, []);

        [DebuggerStepThrough]
        public sealed ICliProcessorConfig AddService(Type serviceType) => AddService(serviceType, []);

        [DebuggerStepThrough]
        public sealed ICliProcessorConfig AddService<T>(
            IEnumerable<CliRouteAttribute> rootRoutes) =>
            AddService(typeof(T), rootRoutes);

        [DebuggerStepThrough]
        public sealed ICliProcessorConfig AddService<T>() =>
            AddService(typeof(T), []);
    }

    sealed record Service(Type ServiceType, object? Instances, ImmutableArray<CliRouteAttribute> RootRoutes);
    sealed class Config : ICliProcessorConfig
    {
        public FluentList<CliGlobalOptionBundle> GlobalOptions { get; } = new();

        public List<Service> Services { get; } = new();

        public ICliProcessorConfig WithProcessorAsService(bool processorIsService)
        {
            this.ProcessorIsService = processorIsService;
            return this;
        }

        public bool ProcessorIsService { get; private set; }

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


    protected CliProcessor(Action<ICliProcessorConfig> initialize)
    {
        var config = new Config();
        initialize.Invoke(config);
        if (config.ProcessorIsService)
        {
            ((ICliProcessorConfig)config).AddService(this);
        }
        
        
        
        var globalOptions = config.GlobalOptions.Distinct();
        _actions = 
            [
                ..config.Services
                    .SelectMany(service =>
                    {
                        var context = new CliContext(service.RootRoutes, globalOptions);
                        return CliMethodInfo
                            .Get(service.ServiceType, context)
                            .Select(method => new CliActionVNext(method, service.Instances));
                    })
            ];
    }


    [DebuggerStepThrough]
    public static CliProcessor CreateDefault(Action<ICliProcessorConfig> initialize)
    {
        return new CliProcessor(initialize);
    }



    private static CliActionVNext[] GetActions(Type type, object? instance, CliContext context)
    {
        var methods = CliMethodInfo
            .Get(type, context);

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
}
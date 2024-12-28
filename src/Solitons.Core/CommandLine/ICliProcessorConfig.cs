using Solitons.Collections;
using Solitons.CommandLine.Reflection;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Solitons.CommandLine;

public interface ICliProcessorConfig
{
    ICliProcessorConfig WithLogo(string logo);
    ICliProcessorConfig WithDescription(string description);
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
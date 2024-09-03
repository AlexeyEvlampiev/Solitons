﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

[DebuggerDisplay("Parameters: {ParametersCount}, Arguments: {ArgumentsCount}, Options: {OptionsCount}")]
internal sealed class CliActionHandlerParametersFactory : ICliCommandMethodParametersFactory
{
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    private readonly List<ICliCommandInputBuilder> _parameterFactories = new();

    public CliActionHandlerParametersFactory(MethodInfo method)
    {
        var methodAttributes = method.GetCustomAttributes(true);
        var routeArguments = methodAttributes.OfType<CliRouteArgumentAttribute>().ToList();
        var parameters = method.GetParameters();

        routeArguments
            .Where(arg => false == parameters.Any(arg.References))
            .Select(arg => arg.ParameterName)
            .Join(",")
            .Convert(missingParamsCsv =>
            {
                if (missingParamsCsv.IsPrintable())
                {
                    throw new InvalidOperationException(
                        $"The parameter(s) '{missingParamsCsv}' specified by CLI route arguments are not found within the method '{method.Name}' parameters.");
                }
            });

        foreach (var parameter in parameters)
        {
            var parameterAttributes = parameter.GetCustomAttributes(true);
            var argument = routeArguments
                .Where(a => a.References(parameter))
                .Do((arg, index) =>
                {
                    if (index > 0)
                    {
                        throw new InvalidOperationException();
                    }
                })
                .LastOrDefault();
            var option = parameterAttributes.OfType<CliOptionAttribute>().SingleOrDefault();

            if (ICliCommandOptionBundle.IsAssignableFrom(parameter.ParameterType))
            {
                var bundleOperand = ThrowIf.NullReference((ICliCommandOptionBundleParameterFactory?)null);
                _parameterFactories.Add(bundleOperand);
            }
            else if(argument is not null)
            {
                var index = Array.IndexOf(parameters, parameter);
                _parameterFactories.Add(new CliCommandParameterArgumentBuilder(index, parameter, argument));
            }
            else
            {
                _parameterFactories.Add(new CliCommandParameterOptionFactory(parameter));
            }
        }
    }

    internal int ArgumentsCount => _parameterFactories.OfType<ICliCommandArgumentBuilder>().Count();
    internal int OptionsCount => OptionFactories.Count();

    internal int ParametersCount => _parameterFactories.Count;

    public object?[] BuildMethodArguments(Match match, ICliTokenSubstitutionPreprocessor preProcessor)
    {
        var args = new object?[_parameterFactories.Count];
        for (int i = 0; i < _parameterFactories.Count; ++i)
        {
            var operand = _parameterFactories[i];
            var value = operand.Build(match, preProcessor);
            args[i] = value;
        }
        return args;
    }


    public IEnumerable<ICliCommandOptionFactory> OptionFactories
    {
        get
        {
            foreach (var factory in _parameterFactories)
            {
                if (factory is ICliCommandOptionFactory parameterFactory)
                {
                    yield return parameterFactory;
                }

                if (factory is ICliCommandOptionBundleParameterFactory bundleFactory)
                {
                    foreach (var propertyFactory in bundleFactory.GetAllOptions())
                    {
                        yield return propertyFactory;
                    }
                }
            }
        }
    }

    public void ForEachOptionBuilder(Action<ICliCommandOptionFactory> action)
    {
        foreach (var builder in _parameterFactories)
        {
            if (builder is ICliCommandOptionFactory parameterOptionBuilder)
            {
                action(parameterOptionBuilder);
            }

            if (builder is ICliCommandOptionBundleParameterFactory bundleBuilder)
            {
                foreach (var propertyOptionBuilder in bundleBuilder.GetAllOptions())
                {
                    action(propertyOptionBuilder);
                }
            }
        }
    }

}
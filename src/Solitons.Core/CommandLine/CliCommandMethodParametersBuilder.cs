using System;
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
                        $"The parameter(s) '{missingParamsCsv}' specified by CLI route arguments are not found " +
                        $"within the method '{method.Name}' parameters.");
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
                        throw new InvalidOperationException(
                            $"The parameter '{parameter.Name}' in method '{method.Name}' is referenced by more than one route argument. " +
                            $"Each parameter should be referenced by at most one route argument.");
                    }
                })
                .LastOrDefault();
            var option = parameterAttributes.OfType<CliOptionAttribute>().SingleOrDefault();
            if (argument is not null && 
                option is not null)
            {
                throw new InvalidOperationException(
                    $"The parameter '{parameter.Name}' in method '{method.Name}' is referenced as a route argument and is also marked as a CLI option. " +
                    $"These two attributes are mutually exclusive. Please correct this.");
            }

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
            else if(option is not null)
            {
                _parameterFactories.Add(new CliCommandParameterOptionFactory(parameter, option));
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }

    internal int ArgumentsCount => _parameterFactories.OfType<ICliCommandArgumentBuilder>().Count();
    internal int OptionsCount => OptionFactories.Count();

    internal int ParametersCount => _parameterFactories.Count;

    public object?[] BuildMethodArguments(Match match, CliTokenDecoder decoder)
    {
        var args = new object?[_parameterFactories.Count];
        for (int i = 0; i < _parameterFactories.Count; ++i)
        {
            var operand = _parameterFactories[i];
            var value = operand.Build(match, decoder);
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
}
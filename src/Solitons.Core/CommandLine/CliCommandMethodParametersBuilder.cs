using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

[DebuggerDisplay("Parameters: {ParametersCount}, Arguments: {ArgumentsCount}, Options: {OptionsCount}")]
internal sealed class CliActionHandlerParametersFactory : ICliCommandMethodParametersBuilder
{
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    private readonly List<ICliCommandInputBuilder> _parameterBuilders = new();

    public CliActionHandlerParametersFactory(MethodInfo method)
    {
        var methodAttributes = method.GetCustomAttributes(true);
        var methodArguments = methodAttributes.OfType<CliArgumentAttribute>().ToList();
        var parameters = method.GetParameters();
        foreach (var parameter in parameters)
        {
            var parameterAttributes = parameter.GetCustomAttributes(true);
            var argument = methodArguments
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
                var bundleOperand = ThrowIf.NullReference((ICliCommandOptionBundleBuilder?)null);
                _parameterBuilders.Add(bundleOperand);
            }
            else if(argument is not null)
            {
                var index = Array.IndexOf(parameters, parameter);
                _parameterBuilders.Add(new CliCommandParameterArgumentBuilder(index, parameter, argument));
            }
            else
            {
                _parameterBuilders.Add(new CliCommandParameterOptionBuilder(parameter));
            }
        }
    }

    internal int ArgumentsCount => _parameterBuilders.OfType<ICliCommandArgumentBuilder>().Count();
    internal int OptionsCount => GetAllCommandOptions().Count();

    internal int ParametersCount => _parameterBuilders.Count;

    public object?[] BuildMethodArguments(Match match, ICliTokenSubstitutionPreprocessor preProcessor)
    {
        var args = new object?[_parameterBuilders.Count];
        for (int i = 0; i < _parameterBuilders.Count; ++i)
        {
            var operand = _parameterBuilders[i];
            var value = operand.Build(match, preProcessor);
            args[i] = value;
        }
        return args;
    }

    public IEnumerable<ICliCommandOptionBuilder> GetAllCommandOptions()
    {
        foreach (var operand in _parameterBuilders)
        {
            if (operand is ICliCommandOptionBuilder option)
            {
                yield return option;
            }

            if (operand is ICliCommandOptionBundleBuilder bundle)
            {
                foreach (var bundleOption in bundle.GetAllOptions())
                {
                    yield return bundleOption;
                }
            }
        }
    }

}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;


internal interface ICliCommandInputBuilder 
{
    Type InputType { get; }

    object? Build(Match commandLineMatch, ICliTokenSubstitutionPreprocessor preProcessor);

}

internal interface ICliCommandSimpleInputBuilder : ICliCommandInputBuilder
{
    protected CliOperandValueParser Parser { get; }

    bool HasDefaultValue(out object? defaultValue);

    string ParameterRegexGroupName { get; }

    CliOptionArity OptionArity { get; }

    bool IsRequired { get; }
    string GetDescription();

    [DebuggerStepThrough]
    public sealed object Parse(Group group, ICliTokenSubstitutionPreprocessor preProcessor) => Parser.Parse(group, preProcessor);

    object? ICliCommandInputBuilder.Build(Match commandLineMatch, ICliTokenSubstitutionPreprocessor preProcessor)
    {
        if (commandLineMatch.Success == false)
        {
            throw new ArgumentException();
        }

        var group = commandLineMatch.Groups[ParameterRegexGroupName];

        if (group.Success == false)
        {
            if (IsRequired)
            {
                CliExit.With("TODO: something like parameter ... is required but was not specified in the command line.");
            }

            if (HasDefaultValue(out var defaultValue))
            {
                if (defaultValue is null)
                {
                    return null;
                }

                var valueType = Nullable.GetUnderlyingType(InputType) ?? InputType;
                if (valueType.IsInstanceOfType(defaultValue))
                {
                    return defaultValue;
                }

                throw new InvalidOperationException();
            }

            CliExit.With(this is ICliCommandOptionFactory opt
                ? $"Required '{opt.OptionExpression}' option value is missing."
                : throw new InvalidOperationException(""));
        }

        return Parse(group, preProcessor);
    }
}

internal interface ICliCommandOptionBundleParameterFactory : ICliCommandInputBuilder
{
    object? ICliCommandInputBuilder.Build(Match commandLineMatch, ICliTokenSubstitutionPreprocessor preProcessor)
    {
        if (false == ICliCommandOptionBundle.IsAssignableFrom(InputType))
        {
            throw new InvalidOperationException();
        }
        var bundle = (ICliCommandOptionBundle)Activator.CreateInstance(this.InputType)!;
        bundle.PopulateFrom(commandLineMatch, preProcessor);
        return bundle;
    }

    IEnumerable<ICliCommandOptionFactory> GetAllOptions();
}

internal interface ICliCommandOptionBundle
{
    void PopulateFrom(Match commandLineMatch, ICliTokenSubstitutionPreprocessor preProcessor);

    public static bool IsAssignableFrom(Type commandInputType)
    {
        return typeof(ICliCommandOptionBundle).IsAssignableFrom(commandInputType);
    }
}

interface ICliCommandParameterBuilder : ICliCommandInputBuilder
{
    string ParameterName { get; }
}

interface ICliCommandPropertyOptionFactory : ICliCommandOptionFactory
{
    string PropertyName { get; }
    Type PropertyType { get; }
}


interface ICliCommandArgumentBuilder : 
    ICliCommandSimpleInputBuilder, 
    ICliCommandParameterBuilder
{
    string ArgumentRole { get; }

    bool ICliCommandSimpleInputBuilder.IsRequired => true;

    CliOptionArity ICliCommandSimpleInputBuilder.OptionArity => CliOptionArity.Scalar;

}

interface ICliCommandOptionFactory : ICliCommandSimpleInputBuilder
{
    public string OptionLongestAlias => OptionAliases
        .OrderByDescending(alias => alias.Length)
        .ThenBy(alias => alias)
        .FirstOrDefault("");

    string OptionExpression { get; }

    IReadOnlyList<string> OptionAliases { get; }
}


internal interface ICliTokenSubstitutionPreprocessor
{
    string GetSubstitution(string key);
}


internal class CliCommandOptionBundleParameterFactory : ICliCommandOptionBundleParameterFactory
{
    private readonly List<ICliCommandOptionFactory> _innerOptionBuilders = new();
    public CliCommandOptionBundleParameterFactory(Type optionBundleType)
    {
        InputType = optionBundleType;
        if (false == ICliCommandOptionBundle.IsAssignableFrom(optionBundleType))
        {
            throw new ArgumentException();
        }

        if (false == InputType.IsClass)
        {
            throw new ArgumentException();
        }

        if (InputType.IsAbstract)
        {
            throw new ArgumentException();
        }

        optionBundleType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ForEach(p =>
            {
                var option = p.GetCustomAttribute<CliOptionAttribute>();
                if (ICliCommandOptionBundle.IsAssignableFrom(p.PropertyType))
                {
                    var builder = new CliCommandOptionBundleParameterFactory(p.PropertyType);
                    _innerOptionBuilders.AddRange(builder.GetAllOptions());
                }
                else if (option is not null)
                {
                    var builder = new CliCommandPropertyOptionFactory(p, option);
                    _innerOptionBuilders.Add(builder);
                }
            });
    }
    public Type InputType { get; }

    public IEnumerable<ICliCommandOptionFactory> GetAllOptions() => _innerOptionBuilders.AsEnumerable();
}

[DebuggerDisplay("{ParameterName} argument")]
internal sealed class CliCommandParameterArgumentBuilder : ICliCommandArgumentBuilder
{
    private readonly ParameterInfo _parameter;
    private readonly CliRouteArgumentAttribute _routeArgument;

    public CliCommandParameterArgumentBuilder(
        int parameterIndex,
        ParameterInfo parameter, 
        CliRouteArgumentAttribute routeArgument)
    {
        _parameter = parameter;
        _routeArgument = routeArgument;
        ThrowIf.ArgumentNull(parameter);
        ThrowIf.ArgumentNull(routeArgument);
        ParameterIndex = parameterIndex;
        ParameterRegexGroupName = parameter.Name!;
        if (false == routeArgument.References(parameter))
        {
            throw new ArgumentException("Oops...", nameof(routeArgument));
        }

        var arity = CliUtils.GetOptionArity(parameter.ParameterType);
        if (arity != CliOptionArity.Scalar)
        {
            throw new InvalidOperationException("Arguments can be only scalars.");
        }
    }

    public CliOperandValueParser Parser { get; }

    public bool HasDefaultValue(out object? defaultValue)
    {
        throw new NotImplementedException();
    }

    public string ParameterRegexGroupName { get; }


    public string GetDescription()
    {
        throw new NotImplementedException();
    }


    public TypeConverter GetValueTypeConverter()
    {
        throw new NotImplementedException();
    }

    public int ParameterIndex { get; }
    public string ParameterName => _parameter.Name!;
    public Type InputType => _parameter.ParameterType;
    public string ArgumentRole => _routeArgument.ArgumentRole;
}

[DebuggerDisplay("{ParameterName} option")]
internal sealed class CliCommandParameterOptionFactory : ICliCommandOptionFactory, ICliCommandParameterBuilder
{
    private readonly ParameterInfo _parameter;
    private readonly CliOptionAttribute _option;

    public CliCommandParameterOptionFactory(ParameterInfo parameter, CliOptionAttribute option)
    {
        _parameter = parameter;
        _option = option;
        OptionArity = CliUtils.GetOptionArity(parameter.ParameterType);
        ParameterRegexGroupName = ThrowIf.ArgumentNullOrWhiteSpace(parameter.Name);
    }

    public IReadOnlyList<string> OptionAliases => _option.Aliases;

    public Type InputType => _parameter.ParameterType;
    public CliOperandValueParser Parser { get; }
    public bool HasDefaultValue(out object? defaultValue)
    {
        throw new NotImplementedException();
    }

    public string ParameterRegexGroupName { get; }
    public CliOptionArity OptionArity { get; }
    public bool IsRequired { get; }

    public string GetDescription()
    {
        throw new NotImplementedException();
    }

    public CliOptionArity GetArity()
    {
        throw new NotImplementedException();
    }

    public string OptionExpression { get; }
    
    public string ParameterName => _parameter.Name;
}



internal sealed class CliCommandPropertyOptionFactory : ICliCommandPropertyOptionFactory
{
    private readonly PropertyInfo _property;
    private readonly CliOptionAttribute _option;

    public CliCommandPropertyOptionFactory(PropertyInfo property, CliOptionAttribute option)
    {
        _property = property;
        _option = option;
        ParameterRegexGroupName = property.Name;
        OptionArity = CliUtils.GetOptionArity(property.PropertyType);
    }

    Type ICliCommandInputBuilder.InputType => PropertyType;
    public CliOperandValueParser Parser { get; }
    public bool HasDefaultValue(out object? defaultValue)
    {
        throw new NotImplementedException();
    }

    public string ParameterRegexGroupName { get; }
    public CliOptionArity OptionArity { get; }
    public bool IsRequired { get; }
    public string GetDescription()
    {
        throw new NotImplementedException();
    }

    public object? Build(Match commandLineMatch, ICliTokenSubstitutionPreprocessor preProcessor)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<ICliCommandOptionFactory> UnnestOptions()
    {
        throw new NotImplementedException();
    }

    public IEnumerable<ICliCommandSimpleInputBuilder> Explode()
    {
        throw new NotImplementedException();
    }

    public string PropertyName => _property.Name;
    public Type PropertyType => _property.PropertyType;
    public string OptionExpression => _option.OptionExpression;
    public IReadOnlyList<string> OptionAliases => _option.Aliases;
}




internal class CliOperandValueParser
{
    private readonly ICliCommandSimpleInputBuilder _operand;

    public CliOperandValueParser(ICliCommandSimpleInputBuilder operand)
    {
        _operand = operand;
    }

    public object Parse(Group group, ICliTokenSubstitutionPreprocessor preProcessor)
    {
        var captures = group.Captures;
        switch (_operand.OptionArity)
        {
            case (CliOptionArity.Flag):
                return Unit.Default;
            case (CliOptionArity.Scalar):
            {
                if (captures.Count > 1)
                {
                    CliExit.With("Ufff...");
                }

                throw new NotImplementedException();
            }
            default:
                throw new NotImplementedException();
        }
    }
}

internal abstract class CliOperand
{
    private readonly object[] _attributes;

    protected CliOperand(ParameterInfo parameter, IReadOnlyList<Attribute> methodAttributes)
        : this((ICustomAttributeProvider)parameter)
    {
        var argument = methodAttributes
            .OfType<CliRouteArgumentAttribute>()
            .Where(a => a.References(parameter))
            .Do((arg, index) =>
            {
                Description = arg.Description;
                if (index > 0)
                {
                    throw new InvalidOperationException("Multiple arguments referencing same parameter");
                }

                if (_attributes.OfType<CliOptionAttribute>().Any())
                {
                    throw new InvalidOperationException("Arguments cannot be declared as options. See method... parameter...");
                }

                if (_attributes.OfType<DescriptionAttribute>().Any())
                {
                    throw new InvalidOperationException("May not overwrite argument description. Remove the Description attribute from ...");
                }
            })
            .LastOrDefault();

        RegexGroupName = parameter.Name.DefaultIfNullOrWhiteSpace($"parameter_{Guid.NewGuid():N}");

    }

    protected CliOperand(PropertyInfo property)
        : this((ICustomAttributeProvider)property)
    {
        RegexGroupName = property.Name.DefaultIfNullOrWhiteSpace($"parameter_{Guid.NewGuid():N}");
    }

    private CliOperand(ICustomAttributeProvider source)
    {
        Description = String.Empty;
        _attributes = source
            .GetCustomAttributes(true)
            .ToArray();

        _attributes
            .OfType<CliOptionAttribute>()
            .ForEach(a => Description = a.Description);

        _attributes
            .OfType<DescriptionAttribute>()
            .ForEach(a => Description = a.Description);

        var type = source is ParameterInfo parameter
            ? parameter.ParameterType
            : source is PropertyInfo property
                ? property.PropertyType
                : throw new InvalidOperationException();
    }
    public string RegexGroupName { get; }
    public string Description { get; private set; } 
}
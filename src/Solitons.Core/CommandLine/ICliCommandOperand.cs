using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;


internal interface ICliCommandOperand 
{
    Type ValueType { get; }

    public object? BuildOperandValue(Match commandLineMatch, ICliTokenSubstitutionPreprocessor preProcessor)
    {
        if (commandLineMatch.Success == false)
        {
            throw new ArgumentException();
        }

        var simpleOperand = ThrowIf.NullReference(this as ICliCommandSimpleTypeOperand);


        var group = commandLineMatch.Groups[simpleOperand.OperandRegexGroupName];

        if (group.Success == false)
        {
            if (simpleOperand.HasDefaultValue(out var defaultValue))
            {
                if (defaultValue is null)
                {
                    return null;
                }

                var valueType = Nullable.GetUnderlyingType(ValueType) ?? ValueType;
                if (valueType.IsInstanceOfType(defaultValue))
                {
                    return defaultValue;
                }

                throw new InvalidOperationException();
            }

            CliExit.With(this is ICliCommandOption opt
                ? $"Required '{opt.OptionAliasesString}' option value is missing."
                : throw new InvalidOperationException(""));
        }

        return simpleOperand.Parse(group, preProcessor);
    }
}

internal interface ICliCommandSimpleTypeOperand : ICliCommandOperand
{
    protected CliOperandValueParser Parser { get; }

    bool HasDefaultValue(out object? defaultValue);

    string OperandRegexGroupName { get; }
    CliOperandArity OperandArity { get; }
    string GetDescription();

    [DebuggerStepThrough]
    public sealed object Parse(Group group, ICliTokenSubstitutionPreprocessor preProcessor) => Parser.Parse(group, preProcessor);
}

internal interface ICliCommandOptionBundleOperand : ICliCommandOperand
{
    
    object? ICliCommandOperand.BuildOperandValue(Match commandLineMatch, ICliTokenSubstitutionPreprocessor preProcessor)
    {
        var bundle = (ICliCommandOptionBundle)Activator.CreateInstance(this.ValueType)!;
        bundle.PopulateFrom(commandLineMatch, preProcessor);
        return bundle;
    }
}

internal interface ICliCommandOptionBundle
{
    void PopulateFrom(Match commandLineMatch, ICliTokenSubstitutionPreprocessor preProcessor);
}

interface ICliCommandMethodParameter : ICliCommandOperand
{
    string ParameterName { get; }
}



interface ICliCommandOptionBundleProperty : ICliCommandOperand
{
    Type PropertyType { get; }
}


interface ICliCommandMethodArgument : ICliCommandSimpleTypeOperand
{
    string ArgumentRole { get; }

    CliOperandArity ICliCommandSimpleTypeOperand.OperandArity => CliOperandArity.Scalar;
}

interface ICliCommandOption : ICliCommandSimpleTypeOperand
{
    CliOperandArity GetArity();
    string OptionAliasesString { get; }
}


internal interface ICliTokenSubstitutionPreprocessor
{
    string GetSubstitution(string key);
}

internal sealed class CliCommandMethodParameterArgument : ICliCommandMethodArgument
{
    public CliCommandMethodParameterArgument(
        int parameterIndex,
        ParameterInfo parameter, 
        CliArgumentAttribute argument)
    {
        ThrowIf.ArgumentNull(parameter);
        ThrowIf.ArgumentNull(argument);
        ParameterIndex = parameterIndex;
        ParameterName = ThrowIf.NullReference(parameter.Name);
        if (false == argument.References(parameter))
        {
            throw new ArgumentException("Oops...", nameof(argument));
        }

        var arity = CliUtils.GetArity(parameter.ParameterType);
        if (arity != CliOperandArity.Scalar)
        {
            throw new InvalidOperationException("Arguments can be only scalars.");
        }
    }

    public CliOperandValueParser Parser { get; }

    public bool HasDefaultValue(out object? defaultValue)
    {
        throw new NotImplementedException();
    }

    public string OperandRegexGroupName { get; }
    public string GetDescription()
    {
        throw new NotImplementedException();
    }


    public TypeConverter GetValueTypeConverter()
    {
        throw new NotImplementedException();
    }

    public int ParameterIndex { get; }
    public string ParameterName { get; }
    public Type ValueType { get; }
    public string ArgumentRole { get; }
}

internal sealed class CliCommandMethodParameterOption : ICliCommandOption, ICliCommandMethodParameter
{
    public CliCommandMethodParameterOption(ParameterInfo parameter)
    {
        
    }
    public Type ValueType { get; }
    public CliOperandValueParser Parser { get; }
    public bool HasDefaultValue(out object? defaultValue)
    {
        throw new NotImplementedException();
    }

    public string OperandRegexGroupName { get; }
    public CliOperandArity OperandArity { get; }
    public string GetDescription()
    {
        throw new NotImplementedException();
    }

    public CliOperandArity GetArity()
    {
        throw new NotImplementedException();
    }

    public string OptionAliasesString { get; }
    public string ParameterName { get; }
}


internal sealed class CliCommandOptionBundleParameter : ICliCommandOptionBundleProperty
{
    public CliCommandOptionBundleParameter(PropertyInfo property)
    {
        
    }

    public Type ValueType { get; }
    public Type PropertyType { get; }
}




internal class CliOperandValueParser
{
    private readonly ICliCommandSimpleTypeOperand _operand;

    public CliOperandValueParser(ICliCommandSimpleTypeOperand operand)
    {
        _operand = operand;
    }

    public object Parse(Group group, ICliTokenSubstitutionPreprocessor preProcessor)
    {
        var captures = group.Captures;
        switch (_operand.OperandArity)
        {
            case (CliOperandArity.Flag):
                return Unit.Default;
            case (CliOperandArity.Scalar):
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
            .OfType<CliArgumentAttribute>()
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


internal sealed class CliCommandParameterCollection : IEnumerable<ICliCommandOperand>
{
    private readonly List<ICliCommandOperand> _operands = new();

    public CliCommandParameterCollection(MethodInfo method)
    {
        var parameters = method.GetParameters();
        foreach (var parameter in parameters)
        {
            if (CliOptionBundle.IsAssignableFrom(parameter.ParameterType))
            {
                var bundleOperand = ThrowIf.NullReference((ICliCommandOptionBundleOperand?)null);
                _operands.Add(bundleOperand);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }

    public object?[] BuildMethodArguments(Match match, ICliTokenSubstitutionPreprocessor preProcessor)
    {
        var args = new object?[_operands.Count];
        for (int i = 0; i < _operands.Count; ++i)
        {
            var operand = _operands[i];
            var value = operand.BuildOperandValue(match, preProcessor);
            args[i] = value;
        }
        return args;
    }

    [DebuggerNonUserCode]
    public IEnumerator<ICliCommandOperand> GetEnumerator() => _operands.GetEnumerator();

    [DebuggerNonUserCode]
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
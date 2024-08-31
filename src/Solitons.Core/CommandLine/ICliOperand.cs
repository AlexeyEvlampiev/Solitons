using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal interface ICliOperand
{
    string GetRegexGroupName();
    CliOperandArity Arity { get; }
    string GetDescription();
    bool HasDefaultValue(out object? defaultValue);
    Type GetValueType();
    TypeConverter GetValueTypeConverter();

    protected sealed object? ExtractValue(Match source)
    {
        if (source.Success == false)
        {
            throw new ArgumentException();
        }

        var group = source.Groups[GetRegexGroupName()];
        if (group.Success == false)
        {
            if (HasDefaultValue(out var defaultValue))
            {
                if (defaultValue is null)
                {
                    return null;
                }

                if (this.GetValueType().IsInstanceOfType(defaultValue))
                {
                    return defaultValue;
                }

                throw new InvalidOperationException();
            }

            CliExit.With(this is ICliOption opt
                ? $"Required '{opt.OptionAliasesString}' option value is missing."
                : throw new InvalidOperationException(""));
        }

        var captures = group.Captures;
        switch (Arity)
        {
            case (CliOperandArity.Flag):
                return Unit.Default;
            case (CliOperandArity.Scalar):
            {
                if (captures.Count > 1)
                {
                    CliExit.With("Ufff...");
                }

                return GetValueTypeConverter().ConvertFromInvariantString(group.Value);
            }
            default:
                return GetValueTypeConverter().ConvertFrom(captures);
        }

    }
}

interface ICliMethodParameter : ICliOperand
{
    int ParameterIndex { get; }

    string ParameterName { get; }

    public sealed void Copy(Match source, object?[] destination)
    {
        if (source.Success == false)
        {
            throw new InvalidOperationException();
        }

        var value = ExtractValue(source);
        destination[ParameterIndex] = value;
    }
}


interface IICliPropertyOption : ICliOperand
{
    protected void Copy(object? value, CliOptionBundle destination);

    [DebuggerStepThrough]
    public sealed void Copy(Match source, CliOptionBundle destination)
    {
        var value = ExtractValue(source);
        Copy(value, destination);
    }
}
interface ICliMethodArgument : ICliMethodParameter
{
    string GetArgumentRole();

    CliOperandArity ICliOperand.Arity => CliOperandArity.Scalar;
}

interface ICliOption : ICliOperand
{
    CliOperandArity GetArity();
    string OptionAliasesString { get; }
}

internal sealed class CliMethodParameterArgument : ICliMethodArgument
{
    private readonly string _regexGroupName;
    private readonly string _description;
    private readonly string _role;
    private readonly Type _valueType;
    public CliMethodParameterArgument(
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

        _regexGroupName = parameter.Name.DefaultIfNullOrWhiteSpace($"parameter_{Guid.NewGuid():N}");
        _role = argument.ArgumentRole;
        _description = argument.Description;
        _valueType = parameter.ParameterType;
        
    }

    string ICliOperand.GetRegexGroupName() => _regexGroupName;

    string ICliOperand.GetDescription() => _description;
    public bool HasDefaultValue(out object? defaultValue)
    {
        throw new NotImplementedException();
    }

    string ICliMethodArgument.GetArgumentRole() => _role;

    Type ICliOperand.GetValueType() => _valueType;

    public TypeConverter GetValueTypeConverter()
    {
        throw new NotImplementedException();
    }

    public int ParameterIndex { get; }
    public string ParameterName { get; }
}

internal sealed class CliMethodParameterOption : ICliOption, ICliMethodParameter
{
    public string GetRegexGroupName()
    {
        throw new NotImplementedException();
    }

    public CliOperandArity Arity { get; }
    public string GetDescription()
    {
        throw new NotImplementedException();
    }

    public bool HasDefaultValue(out object? defaultValue)
    {
        throw new NotImplementedException();
    }

    public Type GetValueType()
    {
        throw new NotImplementedException();
    }

    public TypeConverter GetValueTypeConverter()
    {
        throw new NotImplementedException();
    }

    public CliOperandArity GetArity()
    {
        throw new NotImplementedException();
    }

    public string OptionAliasesString { get; }
    public int ParameterIndex { get; }
    public string ParameterName { get; }
}


internal sealed class CliParameterOption : ICliOption, IICliPropertyOption
{
    public CliParameterOption(PropertyInfo property)
    {
        
    }

    public string GetRegexGroupName()
    {
        throw new NotImplementedException();
    }

    public CliOperandArity Arity { get; }
    public string GetDescription()
    {
        throw new NotImplementedException();
    }

    public bool HasDefaultValue(out object? defaultValue)
    {
        throw new NotImplementedException();
    }

    public Type GetValueType()
    {
        throw new NotImplementedException();
    }

    public TypeConverter GetValueTypeConverter()
    {
        throw new NotImplementedException();
    }

    public CliOperandArity GetArity()
    {
        throw new NotImplementedException();
    }

    public string OptionAliasesString { get; }
    public void Copy(object? value, CliOptionBundle destination)
    {
        throw new NotImplementedException();
    }
}



internal sealed class CliPropertyArgument
{

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
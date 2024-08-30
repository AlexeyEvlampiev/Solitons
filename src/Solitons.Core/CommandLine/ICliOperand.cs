using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Solitons.CommandLine;

internal interface ICliOperand
{
    string GetRegexGroupName();
    string GetDescription();
    Type GetValueType();
    TypeConverter GetValueTypeConverter();
}

interface ICliArgument : ICliOperand
{
    string GetArgumentRole();
}

interface ICliOption : ICliOperand
{
    CliOptionArity GetArity();
}

internal sealed class CliParameterArgument : ICliArgument
{
    private readonly string _regexGroupName;
    private readonly string _description;
    private readonly string _role;
    private readonly Type _valueType;
    public CliParameterArgument(
        ParameterInfo parameter, 
        CliArgumentAttribute argument)
    {
        ThrowIf.ArgumentNull(parameter);
        ThrowIf.ArgumentNull(argument);
        if (false == argument.References(parameter))
        {
            throw new ArgumentException("Oops...", nameof(argument));
        }

        var arity = CliUtils.GetArity(parameter.ParameterType);
        if (arity != CliOptionArity.Scalar)
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

    string ICliArgument.GetArgumentRole() => _role;

    Type ICliOperand.GetValueType() => _valueType;

    public TypeConverter GetValueTypeConverter()
    {
        throw new NotImplementedException();
    }
}

internal sealed class CliParameterOption
{

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
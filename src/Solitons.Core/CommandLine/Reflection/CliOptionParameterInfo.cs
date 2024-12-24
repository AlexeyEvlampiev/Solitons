using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine.Reflection;

internal sealed class CliOptionParameterInfo : CliParameterInfo, ICliOptionMemberInfo
{
    private readonly CliOptionAttribute _optionAttribute;
    private readonly Regex _aliasExactRegex;

    public CliOptionParameterInfo(ParameterInfo parameter) 
        : base(parameter)
    {
        
        var attributes = GetCustomAttributes(true).OfType<Attribute>().ToArray();
        _optionAttribute = CliOptionAttribute.Get(parameter, attributes);



        var underlyingType = Nullable.GetUnderlyingType(ParameterType) ?? ParameterType;
        if (underlyingType == typeof(Unit) ||
            underlyingType == typeof(CliFlag))
        {
            IsFlag = true;
            ValueConverter = null;
        }
        else if (_optionAttribute.CanAccept(underlyingType, out var converter))
        {
            ValueConverter = converter;
        }
        else
        {
            throw new InvalidOperationException();
        }

        if (parameter.IsNullable())
        {
            IsOptional = true;
            if (base.HasDefaultValue == false)
            {
                HasDefaultValue = true;
                DefaultValue = null;
            }
        }
        else if(parameter.HasDefaultValue)
        {
            IsOptional = true;
            DefaultValue = parameter.DefaultValue;
        }


        Aliases = [.. _optionAttribute.Aliases];
        
        _aliasExactRegex = new Regex($"^{_optionAttribute.PipeSeparatedAliases}$");

    }


    public bool IsFlag { get; }

    public TypeConverter? ValueConverter { get; }

    public string Description { get; }

    public bool IsMatch(string optionName) => _aliasExactRegex.IsMatch(optionName);
    public Type OptionType { get; }

    public string PipeSeparatedAliases => _optionAttribute.PipeSeparatedAliases;

    public string OptionAliasesCsv => _optionAttribute.OptionAliasesCsv;

    public ImmutableArray<string> Aliases { get; }

    public bool IsOptionAlias(string arg) => _aliasExactRegex.IsMatch(arg);

    public override bool IsOptional { get; }

    public override bool HasDefaultValue { get; }

    public override object? DefaultValue { get; }

    public override object Parse(string arg)
    {
        if (IsFlag)
        {
            Debug.Assert(ValueConverter is null);
            throw new InvalidOperationException("Oops...");
        }

        var converter = ThrowIf.NullReference(ValueConverter);
        try
        {
            var value = converter.ConvertFrom(arg);
            return ThrowIf.NullReference(value);
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("Oops...");
        }
    }

    public object? GetValue(CliCommandLine commandLine)
    {
        var options = commandLine.Options.Where(o => IsMatch(o.Name)).ToList();
        if (options.Any() == false)
        {
            if (IsOptional)
            {
                return DefaultValue;
            }
        }

        throw new NotImplementedException();
    }
}
using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reflection;
using System.Text.RegularExpressions;
using Solitons.Reflection;

namespace Solitons.CommandLine.Reflection;

internal sealed class CliOptionParameterInfo : ParameterInfoDecorator
{
    private readonly CliOptionAttribute _optionInfo;
    private readonly Regex _aliasExactRegex;

    public CliOptionParameterInfo(ParameterInfo parameter) 
        : base(parameter)
    {
        var attributes = GetCustomAttributes(true).OfType<Attribute>().ToArray();
        bool isNullable;
        if (ParameterType.IsValueType && 
            Nullable.GetUnderlyingType(ParameterType) is not null)
        {
            isNullable = true;
        }
        else
        {
            // Extract nullable-related attributes
            var nullableAttribute = attributes.OfType<System.Runtime.CompilerServices.NullableAttribute>().FirstOrDefault();
            var contextAttribute = attributes.OfType<System.Runtime.CompilerServices.NullableContextAttribute>().FirstOrDefault();

            // Determine nullability based on attribute flags
            var nullableFlag = nullableAttribute?.NullableFlags.FirstOrDefault();
            var contextFlag = contextAttribute?.Flag;

            isNullable = nullableFlag == 2 || contextFlag == 2;
        }

        Description = attributes
                .OfType<DescriptionAttribute>()
                .Select(a => a.Description)
                .Union(attributes.OfType<CliOptionAttribute>().Select(a => a.Description))
                .Union([Name])
                .First(d => d.IsPrintable())!
            ;

        _optionInfo = attributes
                             .OfType<CliOptionAttribute>()
                             .SingleOrDefault()
                         ?? new CliOptionAttribute($"--{Name}", Description);

        var underlyingType = Nullable.GetUnderlyingType(ParameterType) ?? ParameterType;
        if (underlyingType == typeof(Unit) ||
            underlyingType == typeof(CliFlag))
        {
            IsFlag = true;
            ValueConverter = null;
        }
        else if (_optionInfo.CanAccept(underlyingType, out var converter))
        {
            ValueConverter = converter;
        }
        else
        {
            throw new InvalidOperationException();
        }

        if (isNullable)
        {
            IsOptional = true;
            if (base.HasDefaultValue == false)
            {
                HasDefaultValue = true;
                DefaultValue = null;
            }
            
        }


        Aliases = [.. _optionInfo.Aliases];
        _aliasExactRegex = new Regex($"^{_optionInfo.PipeSeparatedAliases}$");
    }



    public bool IsFlag { get; }

    public TypeConverter? ValueConverter { get; }

    public string Description { get; }

    public string PipeSeparatedAliases => _optionInfo.PipeSeparatedAliases;

    public string OptionAliasesCsv => _optionInfo.OptionAliasesCsv;

    public ImmutableArray<string> Aliases { get; }

    public bool IsOptionAlias(string arg) => _aliasExactRegex.IsMatch(arg);

    public override bool IsOptional { get; }

    public override bool HasDefaultValue { get; }

    public override object? DefaultValue { get; }



}
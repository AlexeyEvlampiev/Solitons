using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Solitons.Reflection;

namespace Solitons.CommandLine.Reflection;

internal class CliOptionBundlePropertyInfo : PropertyInfoDecorator,  ICliOptionMemberInfo
{
    private readonly CliOptionAttribute _optionAttribute;
    private readonly CliOptionMaterializer _materializer;

    public CliOptionBundlePropertyInfo(PropertyInfo property) : base(property)
    {
        var attributes = property.GetCustomAttributes().ToList();
        _optionAttribute = attributes.OfType<CliOptionAttribute>().Single();

        IsOptional = 
            false == attributes.OfType<RequiredAttribute>().Any() || 
            property.IsNullable();
        _materializer = CliOptionMaterializer.CreateOrThrow(_optionAttribute, property.PropertyType, IsOptional, null);
    }


    public TypeConverter ValueConverter { get; }
    public bool IsFlag { get; }


    public string Description { get; }

    public string PipeSeparatedAliases => _optionAttribute.PipeSeparatedAliases;

    public string OptionAliasesCsv => _optionAttribute.OptionAliasesCsv;
    public bool IsOptional { get; }
    public object? DefaultValue { get; }

    public ImmutableArray<string> Aliases { get; }



    public bool IsMatch(string optionName) => _optionAttribute.IsMatch(optionName);

    public static bool IsBundleProperty(PropertyInfo propertyInfo)
    {
        return propertyInfo.GetCustomAttribute<CliOptionAttribute>() is not null;
    }

    [DebuggerStepThrough]
    public object? Materialize(CliCommandLine commandLine) => _materializer.Materialize(commandLine);

    public override string ToString() => _optionAttribute.PipeSeparatedAliases;
}
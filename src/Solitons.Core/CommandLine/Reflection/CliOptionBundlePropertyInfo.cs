using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Solitons.Reflection;

namespace Solitons.CommandLine.Reflection;

internal class CliOptionBundlePropertyInfo : PropertyInfoDecorator,  ICliOptionMemberInfo
{
    private readonly CliOptionAttribute _optionAttribute;
    private readonly Regex _aliasExactRegex;

    public CliOptionBundlePropertyInfo(PropertyInfo property) : base(property)
    {
        var attributes = property.GetCustomAttributes().ToList();
        _optionAttribute = attributes.OfType<CliOptionAttribute>().Single();
        _aliasExactRegex = new Regex($"^{_optionAttribute.PipeSeparatedAliases}$");
        IsOptional = attributes.OfType<RequiredAttribute>().Any() == false;
    }


    public bool IsFlag { get; }

    public TypeConverter? ValueConverter { get; }

    public string Description { get; }

    public string PipeSeparatedAliases => _optionAttribute.PipeSeparatedAliases;

    public string OptionAliasesCsv => _optionAttribute.OptionAliasesCsv;
    public bool IsOptional { get; }

    public ImmutableArray<string> Aliases { get; }

    public bool IsOptionAlias(string arg) => _aliasExactRegex.IsMatch(arg);


    public bool IsMatch(string optionName) => _aliasExactRegex.IsMatch(optionName);
}
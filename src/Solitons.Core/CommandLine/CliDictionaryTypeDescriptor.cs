using System;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal sealed record CliDictionaryTypeDescriptor(Type ConcreteType, Type ValueType, bool AcceptsCustomStringComparer) : CliOptionTypeDescriptor
{
    private static readonly Regex MapKeyValueRegex;

    static CliDictionaryTypeDescriptor()
    {
        var pattern = @"(?:\[$key\]\s+$value)|(?:$key\s+$value)"
            .Replace("$key", @"(?<key>\S+)?")
            .Replace("$value", @"(?<value>[^-\s]\S*)?");
        MapKeyValueRegex = new Regex(pattern,
            RegexOptions.Singleline
#if DEBUG
            | RegexOptions.Compiled
#endif
        );
    }

    public static bool IsMatch(string input, out Group key, out Group value)
    {
        var match = MapKeyValueRegex.Match(input);
        key = match.Groups["key"];
        value = match.Groups["value"];
        return match.Success;
    }

    public override TypeConverter GetDefaultTypeConverter() => TypeDescriptor.GetConverter(ValueType);

    public override string CreateRegularExpression(string regexGroupName, string pipeExpression) =>
        $@"(?:{pipeExpression})(?:$dot-notation|$accessor-notation)"
            .Replace(@"$dot-notation", @$"\.(?<{regexGroupName}>(?:\S+\s+[^\s-]\S*)?)")
            .Replace(@"$accessor-notation", @$"(?<{regexGroupName}>(?:\[\S+\]\s+[^\s-]\S*)?)");
}
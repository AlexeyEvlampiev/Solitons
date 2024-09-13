using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal sealed record CliDictionaryTypeDescriptor(Type ConcreteType, Type ValueType) : CliOptionTypeDescriptor
{
    private static readonly Regex MapKeyValueRegex;

    private static readonly CliDictionaryTypeDescriptor Default = new(
        typeof(IDictionary), typeof(object));

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

    public static bool IsMatch(Type optionType, out CliDictionaryTypeDescriptor descriptor)
    {
        descriptor = Default;
        if (optionType.IsGenericType == false)
        {
            return false;
        }

        var args = optionType.GetGenericArguments();
        if (args.Length != 2)
        {
            return false;
        }

        var concreteType = typeof(Dictionary<,>).MakeGenericType([typeof(string), args[1]]);

        if (optionType.IsAssignableFrom(concreteType))
        {
            descriptor = new CliDictionaryTypeDescriptor(concreteType, args[1]);
            return true;
        }

        return false;
    }

    public override TypeConverter GetDefaultTypeConverter() => TypeDescriptor.GetConverter(ValueType);

    public override string CreateRegularExpression(string regexGroupName, string pipeExpression) =>
        $@"(?:{pipeExpression})(?:$dot-notation|$accessor-notation)"
            .Replace(@"$dot-notation", @$"\.(?<{regexGroupName}>(?:\S+\s+[^\s-]\S*)?)")
            .Replace(@"$accessor-notation", @$"(?<{regexGroupName}>(?:\[\S+\]\s+[^\s-]\S*)?)");
}
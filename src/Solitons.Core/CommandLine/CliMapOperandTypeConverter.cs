using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal sealed class CliMapOperandTypeConverter : CliOperandTypeConverter
{
    private readonly string _optionName;
    private readonly TypeConverter _valueTypeConverter;

    public CliMapOperandTypeConverter(
        Type type,
        string optionName)
        : base(true)
    {
        _optionName = optionName;
        ValueType = type.GetGenericArguments()[1];
        _valueTypeConverter = TypeDescriptor.GetConverter(ValueType);
        if (!_valueTypeConverter.CanConvertFrom(typeof(string)))
        {
            throw new InvalidOperationException(
                $"Cannot convert from string to {ValueType}");
        }
    }

    public static bool IsMap(Type type)
    {
        if (!type.IsGenericType) return false;

        var genericTypeDefinition = type.GetGenericTypeDefinition();
        return genericTypeDefinition == typeof(Dictionary<,>) &&
               type.GetGenericArguments()[0] == typeof(string);
    }

    public Type ValueType { get; }

    protected override object Convert(Match match)
    {
        var dictionaryType = typeof(Dictionary<,>).MakeGenericType(typeof(string), ValueType);
        var result = (IDictionary)Activator.CreateInstance(dictionaryType)!;

        var keyValuePairs = match.Groups[_optionName].Captures;
        foreach (Capture capture in keyValuePairs)
        {
            var pair = ThrowIf
                .NullOrWhiteSpace(capture.Value)
                .Convert(s => Regex.Replace(s, @"^\.|\[|\]", ""))
                .Convert(s => Regex.Split(s, @"\s+"));
            if (pair.Length != 2)
            {
                throw new FormatException();
            }

            var (key, valueText) = (pair[0], pair[1]);
            var value = _valueTypeConverter.ConvertFromInvariantString(valueText);
            result.Add(key, value);
        }

        return result;
    }

    public override string ToMatchPattern(string keyPattern)
    {
        var pattern = @$"{keyPattern}(?<{_optionName}>$pair)"
            .Replace("$pair", @"(?:\.$key|\[$key\])(?:\s+[^-]\S*)?")
            .Replace("$key", @"\w+");
        return pattern;
    }
}
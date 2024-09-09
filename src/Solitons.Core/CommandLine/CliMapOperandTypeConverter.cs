using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal sealed class CliMapOperandTypeConverter : CliOperandTypeConverter
{
    private readonly string _optionName;
    private readonly IReadOnlyList<object> _metadata;
    private readonly TypeConverter _valueTypeConverter;

    public CliMapOperandTypeConverter(
        Type type,
        string optionName,
        IReadOnlyList<object> metadata,
        TypeConverter? customTypeConverter) : base(true)
    {
        _optionName = optionName;
        _metadata = metadata;
        ValueType = type.GetGenericArguments()[1];
        _valueTypeConverter = customTypeConverter ?? TypeDescriptor.GetConverter(ValueType);
        if (!_valueTypeConverter.CanConvertFrom(typeof(string)))
        {
            throw new InvalidOperationException(
                $"The specified type '{ValueType.FullName}' cannot be converted from a string. " +
                $"Ensure that a valid type converter is available.");
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

    protected override object Convert(Match match, CliTokenDecoder decoder)
    {
        var dictionaryType = typeof(Dictionary<,>).MakeGenericType(typeof(string), ValueType);
        ICliMapOption? mapOption = _metadata.OfType<ICliMapOption>().FirstOrDefault();

        var result = mapOption is null 
            ? (IDictionary)Activator.CreateInstance(dictionaryType)!
            : (IDictionary)Activator.CreateInstance(dictionaryType, mapOption.GetComparer())!;


        var keyValuePairs = match.Groups[_optionName].Captures;
        foreach (Capture capture in keyValuePairs)
        {
            var pair = ThrowIf
                .NullOrWhiteSpace(capture.Value)
                .Convert(s => Regex.Replace(s, @"^\.|\[|\]", ""))
                .Convert(s => Regex.Split(s, @"\s+"));
            if (pair.Length != 2)
            {
                throw new CliExitException(
                    $"The input '{capture.Value}' does not contain a valid key-value pair. " +
                    $"The issue occurred with the operand '{_optionName}'. Ensure the format is '<key> <value>'."); ;
            }

            var (key, valueText) = (pair[0], pair[1]);
            valueText = decoder(valueText);
            var value = _valueTypeConverter.ConvertFromInvariantString(valueText);
            if (result.Contains(key) && 
                false == result[key]!.Equals(value))
            {
                throw new CliExitException(
                    $"Conflicting specification detected for the parameter '{_optionName}'. " +
                    $"The key '{key}' has multiple conflicting values. Ensure that each key has a unique and consistent value.");
            }
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
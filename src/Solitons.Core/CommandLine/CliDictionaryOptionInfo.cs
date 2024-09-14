using Solitons.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Solitons.CommandLine;

internal sealed record CliDictionaryOptionInfo : CliOptionInfo
{
    private readonly Type _dictionaryType;
    private readonly TypeConverter _valueTypeConverter;
    private static readonly Regex MapKeyValueRegex;

    static CliDictionaryOptionInfo()
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

    private CliDictionaryOptionInfo(
        Config config, 
        Type dictionaryType, 
        Type valueType) : base(config)
    {
        _dictionaryType = dictionaryType;
        ValueType = valueType;
        _valueTypeConverter = 
            OptionMetadata.CanAccept(ValueType, out var converter) && converter.CanConvertFrom(typeof(string)) 
                ? converter
                : throw new InvalidOperationException();
    }

    public Type ValueType { get; }


    public static bool IsMatch(Config config, out CliOptionInfo? result)
    {
        result = null;
        if (config.OptionType.IsGenericType == false)
        {
            return false;
        }

        var args = config.OptionType.GetGenericArguments();
        if (args.Length != 2)
        {
            return false;
        }

        var concreteType = typeof(Dictionary<,>).MakeGenericType([typeof(string), args[1]]);

        if (config.OptionType.IsAssignableFrom(concreteType))
        {
            result = new CliDictionaryOptionInfo(config, concreteType, args[1]);
            return true;
        }

        return false;
    }

    public static bool IsMatch(string input, out Group key, out Group value)
    {
        var match = MapKeyValueRegex.Match(input);
        key = match.Groups["key"];
        value = match.Groups["value"];
        return match.Success;
    }

    public override object Deserialize(Group optionGroup, CliTokenDecoder decoder)
    {
        ThrowIf.ArgumentNull(optionGroup);
        ThrowIf.ArgumentNull(decoder);
        var dictionary = CollectionBuilder.CreateDictionary(_dictionaryType, OptionMetadata.GetValueComparer());

        Debug.WriteLine(dictionary.GetType().FullName);
        foreach (Capture capture in optionGroup.Captures)
        {
            IsMatch(capture.Value, out var keyGroup, out var valueGroup);
            if (keyGroup.Success && valueGroup.Success)
            {
                var key = decoder(keyGroup.Value);
                var input = decoder(valueGroup.Value);

                try
                {
                    var value = _valueTypeConverter.ConvertFromInvariantString(input, ValueType);
                    dictionary[key] = value;
                }
                catch (Exception e) when (e is InvalidOperationException)
                {
                    throw CliConfigurationException
                        .OptionValueConversionFailure(AliasPipeExpression, key, ValueType);
                }
                catch (Exception e) when (e is FormatException or ArgumentException)
                {
                    throw CliExitException
                        .DictionaryOptionValueParseFailure(AliasPipeExpression, key, ValueType);
                }
            }
            else if (keyGroup.Success == valueGroup.Success)
            {
                Debug.Assert(false == keyGroup.Success && false == valueGroup.Success);
                throw CliExitException.InvalidDictionaryOptionKeyValueInput(AliasPipeExpression, capture.Value);
            }
            else if (keyGroup.Success)
            {
                Debug.Assert(valueGroup.Success == false);
                throw CliExitException.DictionaryKeyMissingValue(AliasPipeExpression, keyGroup);
            }
            else if (valueGroup.Success)
            {
                Debug.Assert(keyGroup.Success == false);
                throw CliExitException.DictionaryValueMissingKey(AliasPipeExpression, valueGroup);
            }
        }

        return dictionary;
    }

    protected override string BuildOptionRegularExpression(string pipeExp)
    {
        return $@"(?:{pipeExp})(?:$dot-notation|$accessor-notation)"
            .Replace(@"$dot-notation", @$"\.(?<{RegexMatchGroupName}>(?:\S+\s+[^\s-]\S*)?)")
            .Replace(@"$accessor-notation", @$"(?<{RegexMatchGroupName}>(?:\[\S+\]\s+[^\s-]\S*)?)");
    }
}
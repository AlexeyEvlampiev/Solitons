using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Solitons.Collections;

namespace Solitons.CommandLine;

internal sealed record CliOptionInfo
{
    private static readonly Regex MapKeyValueRegex;

    delegate object? GroupBinder(Group group, CliTokenDecoder decoder);

    private readonly object? _defaultValue;
    private readonly TypeConverter _converter;
    private readonly GroupBinder _groupBinder;


    static CliOptionInfo()
    {
        var pattern = @"(?:\[$key\]\s+$value)|(?:$key\s+$value)"
            .Replace("$key", @"(?<key>\S+)?")
            .Replace("$value", @"(?<value>\S+)?");
        MapKeyValueRegex = new Regex(pattern,
            RegexOptions.Singleline
#if DEBUG
            | RegexOptions.Compiled
#endif
        );
    }


    public CliOptionInfo(
        ICliOptionMetadata metadata,
        object? defaultValue,
        string description,
        Type optionType)
    {
        OptionMetadata = ThrowIf.ArgumentNull(metadata);
        OptionType = optionType;
        OptionUnderlyingType = CliUtils.GetUnderlyingType(optionType);
        Arity = CliUtils.GetOptionArity(optionType);



        if (defaultValue is not null &&
            optionType.IsInstanceOfType(defaultValue) == false)
        {
            throw new CliConfigurationException(
                $"The provided default value is not of type {optionType}. Actual type is {defaultValue.GetType()}");
        }

        _converter = metadata.GetCustomTypeConverter()
                     ?? (Arity == CliOptionArity.Flag ? new CliFlagConverter() : null)
                     ?? (OptionUnderlyingType == typeof(TimeSpan) ? new MultiFormatTimeSpanConverter() : null)
                     ?? (OptionUnderlyingType == typeof(CancellationToken)
                         ? new CliCancellationTokenTypeConverter()
                         : null)
                     ?? TypeDescriptor.GetConverter(OptionUnderlyingType);

        if (_converter.CanConvertFrom(typeof(string)) == false)
        {
            throw new CliConfigurationException(
                $"The '{AliasPipeExpression}' option value tokens cannot be converted from a string to the specified option type '{OptionUnderlyingType}' using the default type converter. " +
                $"To resolve this, correct the option type if it's incorrect, or specify a custom type converter " +
                $"either by inheriting from '{typeof(CliOptionAttribute).FullName}' and overriding '{nameof(CliOptionAttribute.GetCustomTypeConverter)}()', " +
                $"or by applying the '{typeof(TypeConverterAttribute).FullName}' directly on the parameter or property.");
        }



        _defaultValue = defaultValue;
        Aliases = metadata.Aliases;
        Description = description;
        OptionType = optionType;
        RegexMatchGroupName = $"option_{Guid.NewGuid():N}";
        AliasPipeExpression = Aliases.Join("|");
        AliasCsvExpression = Aliases
            .OrderBy(alias => alias.StartsWith("--") ? 1 : 0)
            .ThenBy(alias => alias.Length)
            .Join(",");

        _groupBinder = Arity switch
        {
            (CliOptionArity.Dictionary) => ToDictionary,
            (CliOptionArity.Collection) => ToCollection,
            (CliOptionArity.Value) => ToScalar,
            (CliOptionArity.Flag) => ToFlag,
            _ => throw new InvalidOperationException()
        };


        ThrowIf.NullOrWhiteSpace(AliasPipeExpression);
        switch (Arity)
        {
            case (CliOptionArity.Flag):
                RegularExpression = $@"(?<{RegexMatchGroupName}>{AliasPipeExpression})";
                break;
            case (CliOptionArity.Value):
                RegularExpression = $@"(?:{AliasPipeExpression})\s*(?<{RegexMatchGroupName}>(?:[^\s-]\S*)?)";
                break;
            case (CliOptionArity.Dictionary):
            {
                RegularExpression = $@"(?:{AliasPipeExpression})(?:$dot-notation|$accessor-notation)"
                    .Replace(@"$dot-notation", @$"\.(?<{RegexMatchGroupName}>(?:\S+\s+[^\s-]\S+)?)")
                    .Replace(@"$accessor-notation", @$"(?<{RegexMatchGroupName}>(?:\[\S+\]\s+[^\s-]\S+)?)");
            }
                break;
            default:
                throw new NotSupportedException();
        }
    }

    public ICliOptionMetadata OptionMetadata { get; }

    public string RegularExpression { get; }

    public Type OptionUnderlyingType { get; }


    public CliOptionArity Arity { get; }

    public required bool IsRequired { get; init; }

    internal string RegexMatchGroupName { get; }

    public IReadOnlyList<string> Aliases { get; }

    public string Description { get; }

    public Type OptionType { get; }
    public string CsvDeclaration { get; }

    public string AliasPipeExpression { get; }
    public string AliasCsvExpression { get; }

    public object? Deserialize(Match commandLineMatch, CliTokenDecoder decoder)
    {
        Debug.Assert(commandLineMatch.Success);
        var group = commandLineMatch.Groups[RegexMatchGroupName];
        if (group.Success)
        {
            return _groupBinder.Invoke(group, decoder);
        }

        if (IsRequired)
        {
            CliExit.With($"{AliasPipeExpression} option is required.");
        }

        return _defaultValue;

    }

    private object? ToFlag(Group group, CliTokenDecoder decoder)
    {
        Debug.Assert(group.Success);
        return _converter.ConvertFromInvariantString(group.Value);
    }

    private object? ToScalar(Group group, CliTokenDecoder decoder)
    {
        ThrowIf.ArgumentNull(group);
        ThrowIf.ArgumentNull(decoder);
        ThrowIf.False(group.Success);

        if (group.Captures.Count > 1 &&
            group.Captures
                .Select(c => c.Value)
                .Distinct(StringComparer.Ordinal)
                .Count() > 1)
        {
            CliExit.With(
                $"The option '{AliasPipeExpression}' has multiple conflicting values. Please provide a single value.");
        }

        var input = group.Captures[0].Value;
        try
        {
            return _converter.ConvertFromInvariantString(input, OptionUnderlyingType);
        }
        catch (Exception e) when (e is InvalidOperationException)
        {
            throw new CliConfigurationException(
                $"The option '{AliasPipeExpression}' is misconfigured. " +
                $"The input '{input}' could not be converted to '{OptionUnderlyingType.FullName}'. " +
                "Ensure that a valid type converter is provided.");
        }
        catch (Exception e) when (e is FormatException or ArgumentException)
        {
            // Means the user supplied a wrong input text
            CliExit.With(
                $"Invalid input for option '{AliasPipeExpression}': '{input}' could not be parsed to '{OptionUnderlyingType.FullName}'");
            return null;
        }
    }


    private object? ToCollection(Group group, CliTokenDecoder decoder)
    {
        Debug.Assert(group.Success);
        var inputs = group.Captures.Select(c => c.Value);
        if (OptionMetadata.AllowsCsv)
        {
            inputs = inputs.SelectMany(v => Regex.Split(v, @",").Where(p => p.IsPrintable()));
        }

        var items = inputs
            .Select(text => decoder(text))
            .Select(text =>
            {
                try
                {
                    var item = _converter.ConvertFromInvariantString(text, OptionUnderlyingType);
                    return item;
                }
                catch (Exception e) when (e is InvalidOperationException)
                {
                    throw new CliConfigurationException(
                        $"The option '{AliasPipeExpression}' is misconfigured. " +
                        $"The input tokens could not be converted to '{OptionUnderlyingType.FullName}'. " +
                        "Ensure that a valid type converter is provided.");
                }
                catch (Exception e) when (e is FormatException or ArgumentException)
                {
                    CliExit.With(
                        $"Invalid input for option '{AliasPipeExpression}': token could not be parsed to '{OptionUnderlyingType.FullName}'");
                    return null; // This won't actually return because CliExit.With likely terminates the program
                }
            })
            .Cast<object>()
            .ToList();

        return CollectionBuilder.BuildCollection(OptionType, items);
    }


    private object? ToDictionary(Group group, CliTokenDecoder decoder)
    {
        var dictionaryType = typeof(Dictionary<,>).MakeGenericType(typeof(string), OptionType);
        var dictionary = CollectionBuilder.CreateDictionary(dictionaryType, OptionMetadata.GetDictionaryKeyComparer());
        foreach (Capture capture in group.Captures)
        {
            var match = MapKeyValueRegex.Match(capture.Value);

            var keyGroup = match.Groups["key"];
            var valueGroup = match.Groups["value"];
            if (keyGroup.Success && valueGroup.Success)
            {
                var key = decoder(keyGroup.Value);
                var input = decoder(valueGroup.Value);

                try
                {
                    var value = _converter.ConvertFromInvariantString(input, OptionUnderlyingType);
                    dictionary[key] = value;
                }
                catch (Exception e) when (e is InvalidOperationException)
                {
                    throw new CliConfigurationException(
                        $"The option '{AliasPipeExpression}' is misconfigured. " +
                        $"The input value for key '{key}' could not be converted to '{OptionUnderlyingType.FullName}'. " +
                        "Ensure that a valid type converter is provided.");
                }
                catch (Exception e) when (e is FormatException or ArgumentException)
                {
                    CliExit.With(
                        $"Invalid input for option '{AliasPipeExpression}': '{valueGroup.Value}' could not be parsed to '{OptionUnderlyingType.FullName}'.");
                    return null;
                }
            }
            else if (keyGroup.Success == valueGroup.Success)
            {
                Debug.Assert(false == keyGroup.Success);
                CliExit.With("Key value pair is required");
            }
            else if (keyGroup.Success)
            {
                Debug.Assert(false == valueGroup.Success);
                CliExit.With("Key is required");
            }
            else if (valueGroup.Success)
            {
                Debug.Assert(false == keyGroup.Success);
                CliExit.With("Value is required");
            }
        }

        return dictionary;
    }
}
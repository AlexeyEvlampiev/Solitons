using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Solitons.Collections;

namespace Solitons.CommandLine;

internal sealed record JazzyOptionInfo
{
    private static readonly Regex MapKeyValueRegex;
    delegate object? GroupBinder(Group group, CliTokenDecoder decoder);

    private readonly object? _defaultValue;
    private readonly string _expression;
    private readonly TypeConverter _converter;
    private readonly GroupBinder _groupBinder;


    static JazzyOptionInfo()
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




    public JazzyOptionInfo(
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
            throw new CliConfigurationException($"The provided default value is not of type {optionType}.");
        }

        _converter = metadata.GetCustomTypeConverter() 
                     ?? (Arity == CliOptionArity.Flag ? new CliFlagConverter() : null)
                     ?? (OptionUnderlyingType == typeof(TimeSpan) ? new MultiFormatTimeSpanConverter() : null)
                     ?? (OptionUnderlyingType == typeof(CancellationToken) ? new CliCancellationTokenTypeConverter() : null)
                     ?? TypeDescriptor.GetConverter(OptionUnderlyingType);


        if (_converter.CanConvertTo(OptionUnderlyingType) == false)
        {
            throw new CliConfigurationException(
                $"The option value tokens cannot be converted to the specified option type '{OptionUnderlyingType}' using the default converter. " +
                $"To resolve this, either provide a valid value that matches the option type, " +
                $"fix the option type if it's incorrect, or specify a custom type converter " +
                $"by overriding '{typeof(CliOptionAttribute).FullName}.{nameof(CliOptionAttribute.GetCustomTypeConverter)}()'.");
        }


        _defaultValue = defaultValue;
        Aliases = metadata.Aliases;
        Description = description;
        OptionType = optionType;
        RegexMatchGroupName = $"option_{Guid.NewGuid():N}";
        _expression = Aliases.Join("|");
        
        _groupBinder = Arity switch
        {
            (CliOptionArity.Dictionary) => ToDictionary,
            (CliOptionArity.Collection) => ToCollection,
            (CliOptionArity.Value) => ToScalar,
            (CliOptionArity.Flag) => ToFlag,
            _ => throw new InvalidOperationException()
        };

        var token = Aliases
            .Select(a => a.Trim().ToLower())
            .Distinct(StringComparer.Ordinal)
            .OrderByDescending(a => a.Length)
            .Join("|");
        ThrowIf.NullOrWhiteSpace(token);
        switch (Arity)
        {
            case (CliOptionArity.Flag):
                RegularExpression = $@"(?<{RegexMatchGroupName}>{token})";
                break;
            case (CliOptionArity.Value):
                RegularExpression = $@"(?:{token})\s*(?<{RegexMatchGroupName}>(?:[^\s-]\S*)?)";
                break;
            case (CliOptionArity.Dictionary):
            {
                RegularExpression = $@"(?:{token})(?:$dot-notation|$accessor-notation)"
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

    private string RegexMatchGroupName { get; }

    public IReadOnlyList<string> Aliases { get; }

    public string Description { get; }

    public Type OptionType { get; }
    public string CsvDeclaration { get; }

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
            CliExit.With($"{_expression} option is required.");
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
        Debug.Assert(group.Success);
        if (group.Captures.Count > 1 &&
            group.Captures
                .Select(c => c.Value)
                .Distinct(StringComparer.Ordinal)
                .Count() > 1)
        {
            CliExit.With(
                $"The option '{_expression}' has multiple conflicting values. Please provide a single value.");
        }
        var input = group.Captures[0].Value;
        try
        {
            return _converter.ConvertFromInvariantString(input);
        }
        catch (Exception e)
        {
            CliExit.With(
                $"Invalid input for option '{_expression}': '{input}' could not be converted to the expected type '{OptionUnderlyingType}'.");
            throw;
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
                var item = _converter.ConvertFromInvariantString(text);
                if (item is null)
                {
                    CliExit.With(
                        $"The input '{text}' could not be converted to the expected type '{OptionUnderlyingType}'. " +
                        $"Please provide a valid value.");
                }

                return item;
            })
            .Cast<object>()
            .ToList();

        return CollectionBuilder.BuildCollection(OptionType, items);
    }


    private object? ToDictionary(Group group, CliTokenDecoder decoder)
    {
        var dictionaryType = typeof(Dictionary<,>).MakeGenericType(typeof(string), OptionType);
        var dictionary = CollectionBuilder.CreateDictionary(dictionaryType, OptionMetadata.GetMapKeyComparer());
        foreach (Capture capture in group.Captures)
        {
            var match = MapKeyValueRegex.Match(capture.Value);
            
            var keyGroup = match.Groups["key"];
            var valueGroup = match.Groups["value"];
            if (keyGroup.Success && valueGroup.Success)
            {
                var key = decoder(keyGroup.Value);
                var value = _converter.ConvertFromInvariantString(
                    decoder(valueGroup.Value));
                if (value is null)
                {
                    CliExit.With("Invalid value...");
                }
                dictionary[key] = value;
            }
            else if(keyGroup.Success == valueGroup.Success)
            {
                Debug.Assert(false == keyGroup.Success);
                CliExit.With("Key value pair is required");
            }
            else if(keyGroup.Success)
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
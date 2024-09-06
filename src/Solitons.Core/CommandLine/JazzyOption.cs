using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Solitons.Collections;

namespace Solitons.CommandLine;

internal sealed record JazzyOption
{
    delegate object? Binder(Group group, CliTokenDecoder decoder);
    delegate Match MatchMapOptionPair(string keyValueInput);

    delegate void CollectionItemAppender(object collection, object item);

    private readonly object? _defaultValue;
    private readonly string _expression;
    private readonly TypeConverter _converter;
    private readonly Binder _binder;
    private readonly MatchMapOptionPair _matchMapOptionPair;
    private readonly Func<CollectionBuilder> _dynamicCollectionSuperFactory;


    public JazzyOption(
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
            throw new InvalidOperationException($"The provided default value is not of type {optionType}.");
        }

        _converter = metadata.GetCustomTypeConverter() 
                     ?? (Arity == CliOptionArity.Flag ? new CliFlagConverter() : null)
                     ?? (OptionUnderlyingType == typeof(TimeSpan) ? new MultiFormatTimeSpanConverter() : null)
                     ?? (OptionUnderlyingType == typeof(CancellationToken) ? new CliCancellationTokenTypeConverter() : null)
                     ?? TypeDescriptor.GetConverter(OptionUnderlyingType);


        if (_converter.CanConvertTo(OptionUnderlyingType) == false)
        {
            throw new InvalidOperationException(
                $"The option value tokens cannot be converted to the specified option type '{OptionUnderlyingType}' using the default converter. " +
                $"To resolve this, either provide a valid value that matches the option type, " +
                $"fix the option type if it's incorrect, or specify a custom type converter " +
                $"by overriding '{typeof(CliOptionAttribute).FullName}.{nameof(CliOptionAttribute.GetCustomTypeConverter)}()'.");
        }

        var lazyMapRegex = new Lazy<Regex>(BuildMapOptionPairRegex);
        _matchMapOptionPair = input => lazyMapRegex.Value.Match(input);

        _defaultValue = defaultValue;
        Aliases = metadata.Aliases;
        Description = description;
        OptionType = optionType;
        RegexMatchGroupName = $"option_{Guid.NewGuid():N}";
        _expression = Aliases.Join("|");
        
        _binder = Arity switch
        {
            (CliOptionArity.Map) => ToDictionary,
            (CliOptionArity.Vector) => ToCollection,
            (CliOptionArity.Scalar) => ToScalar,
            (CliOptionArity.Flag) => ToFlag,
            _ => throw new InvalidOperationException()
        };

        _dynamicCollectionSuperFactory = Arity == CliOptionArity.Vector
            ? () => new CollectionBuilder(OptionType)
            : () => new CollectionBuilder(typeof(object[]));
    }

    public ICliOptionMetadata OptionMetadata { get; }

    public Type OptionUnderlyingType { get; }


    public CliOptionArity Arity { get; }

    public required bool IsRequired { get; init; }

    private string RegexMatchGroupName { get; }
    public IReadOnlyList<string> Aliases { get; }
    public string Description { get; }
    public Type OptionType { get; }

    public object? Bind(Match commandLineMatch, CliTokenDecoder decoder)
    {
        Debug.Assert(commandLineMatch.Success);
        var group = commandLineMatch.Groups[RegexMatchGroupName];
        if (group.Success)
        {
            return _binder.Invoke(group, decoder);
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

        var factory = _dynamicCollectionSuperFactory();
        inputs
            .Select(text => decoder(text))
            .ForEach(text =>
            {
                var item = _converter.ConvertFromInvariantString(text);
                if (item is null)
                {
                    CliExit.With(
                        $"The input '{text}' could not be converted to the expected type '{OptionUnderlyingType}'. " +
                        $"Please provide a valid value.");
                    return;
                }

                factory.Add(item);
            });

        return factory.Build();
    }


    private Regex BuildMapOptionPairRegex()
    {
        // Has to match things like '.key value' or '[key] value'
        return new Regex(
            @"(?:\[$key\]\s+$value)|(?:$key\s+$value)"
                .Replace("$key", @"(?<key>\S+)?")
                .Replace("$value", @"(?<value>\S+)?"),
            RegexOptions.Compiled |
            RegexOptions.Singleline);
    }

    private object? ToDictionary(Group group, CliTokenDecoder decoder)
    {
        var dictionaryType = typeof(Dictionary<,>).MakeGenericType(typeof(string), OptionType);
        var dictionary = (IDictionary)Activator.CreateInstance(dictionaryType, OptionMetadata.GetMapKeyComparer())!;
        foreach (Capture capture in group.Captures)
        {
            var match = _matchMapOptionPair.Invoke(capture.Value);
            
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Solitons.Collections;
using System.Reactive;

namespace Solitons.CommandLine;

internal abstract record CliOptionTypeDescriptor
{
    public abstract TypeConverter GetDefaultTypeConverter();
}

internal sealed record CliFlagOptionTypeDescriptor(Type FlagType) : CliOptionTypeDescriptor
{
    public override TypeConverter GetDefaultTypeConverter() => new CliFlagConverter();
}

internal sealed record CliValueOptionTypeDescriptor(Type ValueType) : CliOptionTypeDescriptor
{
    public override TypeConverter GetDefaultTypeConverter() => TypeDescriptor.GetConverter(ValueType);
}

internal sealed record CliCollectionOptionTypeDescriptor(Type ConcreteType, Type ItemType, bool AcceptsCustomEqualityComparer) : CliOptionTypeDescriptor
{
    public override TypeConverter GetDefaultTypeConverter() => TypeDescriptor.GetConverter(ItemType);
}

internal sealed record CliDictionaryTypeDescriptor(Type ConcreteType, Type ValueType, bool AcceptsCustomStringComparer) : CliOptionTypeDescriptor
{
    public override TypeConverter GetDefaultTypeConverter() => TypeDescriptor.GetConverter(ValueType);
}

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
            .Replace("$value", @"(?<value>[^-\s]\S*)?");
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
        OptionType = optionType = Nullable.GetUnderlyingType(optionType) ?? optionType;
        TypeDescriptor = GetOptionTypeDescriptor(optionType);



        if (defaultValue is not null &&
            optionType.IsInstanceOfType(defaultValue) == false)
        {
            throw new CliConfigurationException(
                $"The provided default value is not of type {optionType}. Actual type is {defaultValue.GetType()}");
        }

        var customConverter = metadata.GetCustomTypeConverter(out var inputSample);
        if (customConverter is not null &&
            customConverter.CanConvertFrom(typeof(string)))
        {
            try
            {
                var value = customConverter.ConvertFromInvariantString(inputSample);
                if (value is null)
                {
                    throw new CliConfigurationException(
                        $"The sample value '{inputSample}' for the option '{AliasPipeExpression}' cannot be converted using the specified custom converter '{customConverter.GetType().FullName}'. " +
                        $"Ensure that the converter is compatible with the option type '{OptionType.FullName}', " +
                        $"or update the option type to align with the converter.");

                }

                if (false == OptionType.IsInstanceOfType(value))
                {
                    throw new CliConfigurationException(
                        $"The converted value of type '{value.GetType().FullName}' is not compatible with the expected option type '{OptionType.FullName}' " +
                        $"for the '{AliasPipeExpression}' option. " +
                        $"Verify that the custom converter produces values that match the expected type.");
                }
            }
            catch (Exception e) when(e is not CliConfigurationException)
            {
                throw new CliConfigurationException(
                    $"An error occurred while converting the sample value '{inputSample}' for the option '{AliasPipeExpression}' using the custom converter '{customConverter.GetType().FullName}'. " +
                    $"See inner exception for more details.", e);
            }
        }

        _converter = customConverter
                     ?? (TypeDescriptor is CliFlagOptionTypeDescriptor ? new CliFlagConverter() : null)
                     ?? (OptionType == typeof(TimeSpan) ? new MultiFormatTimeSpanConverter() : null)
                     ?? (OptionType == typeof(CancellationToken)
                         ? new CliCancellationTokenTypeConverter()
                         : null)
                     ?? TypeDescriptor.GetDefaultTypeConverter();

        if (_converter.CanConvertFrom(typeof(string)) == false)
        {
            throw new CliConfigurationException(
                $"The '{AliasPipeExpression}' option value tokens cannot be converted from a string to the specified option type '{OptionType}' using the default type converter. " +
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

        _groupBinder = TypeDescriptor switch
        {
            (CliDictionaryTypeDescriptor) => ToDictionary,
            (CliCollectionOptionTypeDescriptor) => ToCollection,
            (CliValueOptionTypeDescriptor) => ToValue,
            (CliFlagOptionTypeDescriptor) => ToFlag,
            _ => throw new InvalidOperationException()
        };


        ThrowIf.NullOrWhiteSpace(AliasPipeExpression);
        RegularExpression = TypeDescriptor switch
        {
            (CliDictionaryTypeDescriptor) => $@"(?:{AliasPipeExpression})(?:$dot-notation|$accessor-notation)"
                .Replace(@"$dot-notation", @$"\.(?<{RegexMatchGroupName}>(?:\S+\s+[^\s-]\S+)?)")
                .Replace(@"$accessor-notation", @$"(?<{RegexMatchGroupName}>(?:\[\S+\]\s+[^\s-]\S+)?)"),
            (CliCollectionOptionTypeDescriptor) => $@"(?:{AliasPipeExpression})\s*(?<{RegexMatchGroupName}>(?:[^\s-]\S*)?)",
            (CliValueOptionTypeDescriptor) => $@"(?:{AliasPipeExpression})\s*(?<{RegexMatchGroupName}>(?:[^\s-]\S*)?)",
            (CliFlagOptionTypeDescriptor) => $@"(?<{RegexMatchGroupName}>{AliasPipeExpression})",
            _ => throw new InvalidOperationException()
        };
    }

    public ICliOptionMetadata OptionMetadata { get; }

    public string RegularExpression { get; }

    public CliOptionTypeDescriptor TypeDescriptor { get; }


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

    private object? ToValue(Group group, CliTokenDecoder decoder)
    {
        ThrowIf.ArgumentNull(group);
        ThrowIf.ArgumentNull(decoder);
        ThrowIf.False(group.Success);
        var descriptor = ThrowIf.NullReference(TypeDescriptor as CliValueOptionTypeDescriptor);

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
            return _converter.ConvertFromInvariantString(input, descriptor.ValueType);
        }
        catch (Exception e) when (e is InvalidOperationException)
        {
            throw new CliConfigurationException(
                $"The option '{AliasPipeExpression}' is misconfigured. " +
                $"The input '{input}' could not be converted to '{descriptor.ValueType}'. " +
                "Ensure that a valid type converter is provided.");
        }
        catch (Exception e) when (e is FormatException or ArgumentException)
        {
            // Means the user supplied a wrong input text
            CliExit.With(
                $"Invalid input for option '{AliasPipeExpression}': given token could not be parsed to '{descriptor.ValueType}'");
            return null;
        }
    }


    private object? ToCollection(Group group, CliTokenDecoder decoder)
    {
        ThrowIf.ArgumentNull(group);
        ThrowIf.ArgumentNull(decoder);
        var descriptor = ThrowIf.NullReference(TypeDescriptor as CliCollectionOptionTypeDescriptor);
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
                    var item = _converter.ConvertFromInvariantString(text, descriptor.ItemType);
                    return item;
                }
                catch (Exception e) when (e is InvalidOperationException)
                {
                    throw new CliConfigurationException(
                        $"The option '{AliasPipeExpression}' is misconfigured. " +
                        $"The input tokens could not be converted to '{descriptor.ItemType}'. " +
                        "Ensure that a valid type converter is provided.");
                }
                catch (Exception e) when (e is FormatException or ArgumentException)
                {
                    CliExit.With(
                        $"Invalid input for option '{AliasPipeExpression}': token could not be parsed to '{descriptor.ItemType.FullName}'");
                    return null; // This won't actually return because CliExit.With likely terminates the program
                }
            })
            .Cast<object>()
            .ToList();

        return CollectionBuilder.BuildCollection(OptionType, items);
    }


    private object? ToDictionary(Group group, CliTokenDecoder decoder)
    {
        ThrowIf.ArgumentNull(group);
        ThrowIf.ArgumentNull(decoder);
        var descriptor = ThrowIf.NullReference(TypeDescriptor as CliDictionaryTypeDescriptor);
        var dictionary = CollectionBuilder.CreateDictionary(descriptor.ConcreteType, OptionMetadata.GetDictionaryKeyComparer());

        Debug.WriteLine(dictionary.GetType().FullName);
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
                    var value = _converter.ConvertFromInvariantString(input, descriptor.ValueType);
                    dictionary[key] = value;
                }
                catch (Exception e) when (e is InvalidOperationException)
                {
                    throw new CliConfigurationException(
                        $"The option '{AliasPipeExpression}' is misconfigured. " +
                        $"The input value for key '{key}' could not be converted to '{descriptor.ValueType}'. " +
                        "Ensure that a valid type converter is provided.");
                }
                catch (Exception e) when (e is FormatException or ArgumentException)
                {
                    CliExit.With(
                        $"Invalid input for option '{AliasPipeExpression}': '{valueGroup.Value}' could not be parsed to '{descriptor.ValueType}'.");
                    return null;
                }
            }
            else if (keyGroup.Success == valueGroup.Success)
            {
                Debug.Assert(false == keyGroup.Success && false == valueGroup.Success);
                CliExit.With(
                    $"Invalid input for option '{AliasPipeExpression}'. " +
                    $"Expected a key-value pair but received '{capture.Value}'. Please provide both a key and a value.");
            }
            else if (keyGroup.Success)
            {
                Debug.Assert(valueGroup.Success == false);
                CliExit.With(
                    $"A value is missing for the key '{keyGroup.Value}' in option '{AliasPipeExpression}'. " +
                    "Please specify a corresponding value.");
            }
            else if (valueGroup.Success)
            {
                Debug.Assert(keyGroup.Success == false);
                CliExit.With(
                    $"A key is missing for the value '{valueGroup.Value}' in option '{AliasPipeExpression}'. " +
                    "Please specify a corresponding key.");
            }
        }

        return dictionary;
    }


    public static CliOptionTypeDescriptor GetOptionTypeDescriptor(Type optionType, Action<string>? raiseArgumentError = null)
    {
        raiseArgumentError ??= (message) => Debug.Fail(message);
        raiseArgumentError += (msg) => throw new ArgumentException(msg, nameof(optionType));

        optionType = Nullable.GetUnderlyingType(optionType) ?? optionType;

        var dictionaryInterfaceType = optionType
            .GetInterfaces()
            .Where(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            .MinBy(t => t.GetGenericArguments().First() == typeof(string) ? 0 : 1);

        if (dictionaryInterfaceType is null && 
            optionType.IsInterface && 
            optionType.IsGenericType && typeof(IDictionary<,>).IsAssignableFrom(optionType.GetGenericTypeDefinition()))
        {
            dictionaryInterfaceType = optionType;
        }
        if (dictionaryInterfaceType is not null)
        {
            var args = dictionaryInterfaceType.GetGenericArguments();
            var (keyType, valueType) = (args[0], args[1]);
            if (keyType == typeof(string))
            {
                if (optionType.IsInterface)
                {
                    var dictionaryType = typeof(Dictionary<,>).MakeGenericType([keyType, valueType]);
                    return new CliDictionaryTypeDescriptor(dictionaryType, valueType, true);

                }

                if (optionType.IsAbstract)
                {
                    raiseArgumentError("Abstract dictionary types not supported");
                }

                var ctor = optionType.GetConstructor(Type.EmptyTypes);
                var cmpCtor = optionType.GetConstructor([typeof(StringComparer)]);
                if (ctor is not null || cmpCtor is not null)
                {
                    return new CliDictionaryTypeDescriptor(optionType, valueType, cmpCtor is not null);
                }

                raiseArgumentError("No suitable constructor found for the dictionary type.");
            }

            raiseArgumentError("Dictionary key type must be a string.");
        }

        var collectionInterfaceType = optionType
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType &&
                                 i.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
                                 i != typeof(IEnumerable<string>));


        


        if (collectionInterfaceType is not null)
        {
            var itemType = collectionInterfaceType.GetGenericArguments()[0];
            if (!optionType.IsAbstract)
            {
                var ctor = optionType.GetConstructor(Type.EmptyTypes);
                if (ctor != null)
                {
                    return new CliCollectionOptionTypeDescriptor(optionType, itemType, false);
                }

                raiseArgumentError("No suitable constructor found for the collection type.");
            }
            else
            {
                var listType = typeof(List<>).MakeGenericType(itemType);
                return new CliCollectionOptionTypeDescriptor(listType, itemType, false);
            }
        }

        if (optionType == typeof(Unit) || optionType == typeof(CliFlag))
        {
            return new CliFlagOptionTypeDescriptor(optionType);
        }

        if (!optionType.IsAbstract)
        {
            return new CliValueOptionTypeDescriptor(optionType);
        }

        raiseArgumentError("Unsupported option type.");
        throw new InvalidOperationException(); // Just to satisfy method return
    }
}
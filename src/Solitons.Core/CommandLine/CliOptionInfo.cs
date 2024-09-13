using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Solitons.Collections;
using System.Reactive;
using Solitons.Caching;
namespace Solitons.CommandLine;

internal sealed record CliOptionInfo
{
    private static readonly Regex MapKeyValueRegex;

    delegate object? GroupBinder(Group group, CliTokenDecoder decoder);

    private readonly object? _defaultValue;
    private readonly IInMemoryCache _cache;
    private readonly TypeConverter _converter;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly GroupBinder _groupBinder;
    private readonly object? _customInputSample;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly TypeConverter _customTypeConverter;


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
        string name,
        object? defaultValue,
        string description,
        Type optionType,
        IInMemoryCache cache)
    {
        OptionMetadata = ThrowIf.ArgumentNull(metadata);
        OptionType = optionType = Nullable.GetUnderlyingType(optionType) ?? optionType;
        _defaultValue = defaultValue;
        _cache = cache;
        Aliases = metadata.Aliases;
        Description = description;
        OptionType = optionType;
        RegexMatchGroupName = $"option_{name}_{Guid.NewGuid():N}";
        AliasPipeExpression = Aliases.Join("|");
        AliasCsvExpression = Aliases
            .OrderBy(alias => alias.StartsWith("--") ? 1 : 0)
            .ThenBy(alias => alias.Length)
            .Join(",");

        
        if (metadata.HasCustomTypeConverter(
                out var customConverter, 
                out var inputSample))
        {
            _customTypeConverter = customConverter;
            try
            {
                _customInputSample = customConverter.ConvertFromInvariantString(inputSample);
                if (_customInputSample is null)
                {
                    throw new CliConfigurationException(
                        $"The sample value '{inputSample}' for the option '{AliasPipeExpression}' cannot be converted using the specified custom converter '{customConverter.GetType().FullName}'. " +
                        $"Ensure that the converter is compatible with the option type '{OptionType.FullName}', " +
                        $"or update the option type to align with the converter.");
                }
            }
            catch (Exception e)
            {
                throw new CliConfigurationException("Oops");
            }
        }

        TypeDescriptor = ToOptionTypeDescriptor();



        if (defaultValue is not null &&
            optionType.IsInstanceOfType(defaultValue) == false)
        {
            throw new CliConfigurationException(
                $"The provided default value is not of type {optionType}. Actual type is {defaultValue.GetType()}");
        }

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

            }
            catch (Exception e) when(e is not CliConfigurationException)
            {
                throw new CliConfigurationException(
                    $"An error occurred while converting the sample value '{inputSample}' for the option '{AliasPipeExpression}' using the custom converter '{customConverter.GetType().FullName}'. " +
                    $"See inner exception for more details.", e);
            }
        }

        _converter = customConverter
                     ?? (TypeDescriptor is CliFlagOptionTypeDescriptor flagDesc ? flagDesc.GetDefaultTypeConverter() : null)
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
                $"either by inheriting from '{typeof(CliOptionAttribute).FullName}' and overriding '{nameof(CliOptionAttribute.HasCustomTypeConverter)}()', " +
                $"or by applying the '{typeof(TypeConverterAttribute).FullName}' directly on the parameter or property.");
        }



        

        _groupBinder = TypeDescriptor switch
        {
            (CliDictionaryTypeDescriptor) => ToDictionary,
            (CliCollectionOptionTypeDescriptor) => ToCollection,
            (CliValueOptionTypeDescriptor) => ToValue,
            (CliFlagOptionTypeDescriptor) => ToFlag,
            _ => throw new InvalidOperationException()
        };


        ThrowIf.NullOrWhiteSpace(AliasPipeExpression);
        var pipeExp = AliasPipeExpression.Replace("?", "[?]");
        RegularExpression = TypeDescriptor.CreateRegularExpression(RegexMatchGroupName, pipeExp);
    }

    public ICliOptionMetadata OptionMetadata { get; }

    public string RegularExpression { get; }

    public CliOptionTypeDescriptor TypeDescriptor { get; }


    public required bool IsRequired { get; init; }

    internal string RegexMatchGroupName { get; }

    public IReadOnlyList<string> Aliases { get; }

    public string Description { get; }

    public Type OptionType { get; }

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

        var input = decoder(group.Captures[0].Value);
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

        var comparer = OptionMetadata.GetValueComparer();
        return CollectionBuilder.BuildCollection(OptionType, items, comparer);
    }


    private object? ToDictionary(Group group, CliTokenDecoder decoder)
    {
        ThrowIf.ArgumentNull(group);
        ThrowIf.ArgumentNull(decoder);
        var descriptor = ThrowIf.NullReference(TypeDescriptor as CliDictionaryTypeDescriptor);
        var dictionary = CollectionBuilder.CreateDictionary(descriptor.ConcreteType, OptionMetadata.GetValueComparer());

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


    public CliOptionTypeDescriptor ToOptionTypeDescriptor()
    {
        Debug.Assert(AliasPipeExpression.IsPrintable());
        Debug.Assert(AliasCsvExpression.IsPrintable());
        Debug.Assert(OptionType is not null);
        return
            AsDictionaryTypeDescriptor() ??
            AsCollectionTypeDescriptor() ??
            AsValueTypeDescriptor() ??
            AsFlagTypeDescriptor() ??
            throw new NotSupportedException($"The type '{OptionType.FullName}' is not supported by the CLI option system.");
    }

    private CliOptionTypeDescriptor? AsFlagTypeDescriptor()
    {
        if (OptionType == typeof(Unit) || 
            OptionType == typeof(CliFlag))
        {
            return new CliFlagOptionTypeDescriptor(OptionType);
        }

        return null;
    }


    private CliOptionTypeDescriptor? AsValueTypeDescriptor()
    {
        if (OptionType == typeof(Unit) ||
            OptionType == typeof(CliFlag) ||
            OptionType.IsAbstract)
        {
            return null;
        }

        return new CliValueOptionTypeDescriptor(OptionType);
    }


    private CliOptionTypeDescriptor? AsCollectionTypeDescriptor()
    {
        ThrowIf.NullReference(OptionType);
        ThrowIf.NullOrWhiteSpace(AliasPipeExpression);

        if (CliCollectionOptionTypeDescriptor.IsMatch(
                OptionType, 
                out var descriptor))
        {
            if (_customInputSample is not null &&
                descriptor.ItemType.IsInstanceOfType(_customInputSample) == false)
            {
                throw CliConfigurationException
                    .OptionCollectionItemTypeMismatch(
                        AliasPipeExpression, 
                        _customInputSample.GetType(), 
                        descriptor.ItemType);
            }

            return descriptor;
        }

        if (OptionType == typeof(string) ||
            OptionType == typeof(Unit) ||
            OptionType == typeof(CliFlag) ||
            OptionType.IsEnum ||
            typeof(IDictionary).IsAssignableFrom(OptionType) ||
            typeof(IEnumerable).IsAssignableFrom(OptionType) == false)
        {
            Debug.WriteLine($"{OptionType} is not a collection or an incompatible type.");
            return null;
        }

        Debug.WriteLine($"{OptionType} is assignable from {typeof(IEnumerable)}");
        throw CliConfigurationException
            .UnsupportedOptionCollectionType(AliasPipeExpression, OptionType);
    }

    private CliOptionTypeDescriptor? AsDictionaryTypeDescriptor()
    {
        var dictionaryInterfaceType = OptionType
            .GetInterfaces()
            .Where(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            .MinBy(t => t.GetGenericArguments().First() == typeof(string) ? 0 : 1);

        if (dictionaryInterfaceType is null &&
            OptionType.IsInterface &&
            OptionType.IsGenericType && typeof(IDictionary<,>).IsAssignableFrom(OptionType.GetGenericTypeDefinition()))
        {
            dictionaryInterfaceType = OptionType;
        }

        if (dictionaryInterfaceType is null)
        {
            return null;
        }

        var args = dictionaryInterfaceType.GetGenericArguments();
        var (keyType, valueType) = (args[0], args[1]);
        if (keyType != typeof(string))
        {
            throw new ArgumentException("Dictionary key type must be a string.", nameof(OptionType));
        }
            
        if (OptionType.IsInterface)
        {
            var dictionaryType = typeof(Dictionary<,>).MakeGenericType([keyType, valueType]);
            return new CliDictionaryTypeDescriptor(dictionaryType, valueType, true);

        }

        if (OptionType.IsAbstract)
        {
            throw new ArgumentException("Abstract dictionary types not supported", nameof(OptionType));
        }

        var ctor = OptionType.GetConstructor(Type.EmptyTypes);
        var cmpCtor = OptionType.GetConstructor([typeof(StringComparer)]);
        if (ctor is not null || cmpCtor is not null)
        {
            return new CliDictionaryTypeDescriptor(OptionType, valueType, cmpCtor is not null);
        }

        throw new ArgumentException("No suitable constructor found for the dictionary type.", nameof(OptionType));

    }
}
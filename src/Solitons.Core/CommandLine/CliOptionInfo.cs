using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System;
using System.ComponentModel;
using System.Linq;
using Solitons.Collections;
namespace Solitons.CommandLine;

internal abstract record CliOptionInfo
{
    delegate object? GroupBinder(Group group, CliTokenDecoder decoder);

    internal sealed record Config
    {
        public required ICliOptionMetadata Metadata { get; init; }

        public required object? DefaultValue { get; init; }

        public required string Name { get; init; }

        public required string Description { get; init; }

        public required Type OptionType { get; init; }

        public required bool IsRequired { get; init; }
    };

    private readonly object? _defaultValue;
    private readonly TypeConverter _converter;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly GroupBinder _groupBinder;

    protected CliOptionInfo(
        Config config)
    {
        OptionMetadata = ThrowIf.ArgumentNull(config.Metadata);
        OptionType = Nullable.GetUnderlyingType(config.OptionType) ?? config.OptionType;
        _defaultValue = config.DefaultValue;
        Aliases = config.Metadata.Aliases;
        Description = config.Description;
        RegexMatchGroupName = $"option_{config.Name}_{Guid.NewGuid():N}";
        AliasPipeExpression = Aliases.Join("|");
        AliasCsvExpression = Aliases
            .OrderBy(alias => alias.StartsWith("--") ? 1 : 0)
            .ThenBy(alias => alias.Length)
            .Join(",");

        IsRequired = config.IsRequired;


        if (metadata.HasCustomTypeConverter(
                out var customConverter, 
                out var inputSample))
        {
            _converter = customConverter;
            try
            {
                var sampleValue = ThrowIf.NullReference(customConverter
                    .ConvertFromInvariantString(inputSample));
            }
            catch (Exception e)
            {
                throw CliConfigurationException
                    .OptionSampleConversionWithCustomConverterFailed(
                        AliasPipeExpression,
                        inputSample,
                        OptionType,
                        customConverter.GetType());
            }
        }


        if (CliFlagOptionTypeDescriptor.IsMatch(OptionType, out var flag))
        {
            TypeDescriptor = flag;
            _groupBinder = ToFlag;
            _converter = flag.GetDefaultTypeConverter();
        }
        else if (CliDictionaryTypeDescriptor.IsMatch(OptionType, out var dictionary))
        {
            TypeDescriptor = dictionary;
            _groupBinder = ToDictionary;
            var valueType = Nullable.GetUnderlyingType(dictionary.ValueType) ?? dictionary.ValueType;
            _converter ??= valueType == typeof(TimeSpan) 
                ? new MultiFormatTimeSpanConverter() 
                : System.ComponentModel.TypeDescriptor.GetConverter(valueType);
            if (checkTypeCompatibility(valueType) == false)
            {
                throw CliConfigurationException
                    .OptionDictionaryValueTypeMismatch(
                        AliasPipeExpression,
                        _converter.GetType(),
                        dictionary.ValueType);
            }
        }
        else if (CliCollectionOptionTypeDescriptor.IsMatch(OptionType, out var collection))
        {
            TypeDescriptor = collection;
            _groupBinder = ToCollection;
            var itemType = Nullable.GetUnderlyingType(collection.ItemType) ?? collection.ItemType;
            _converter ??=
                (itemType == typeof(TimeSpan) ? new MultiFormatTimeSpanConverter() : null) ??
                System.ComponentModel.TypeDescriptor.GetConverter(itemType);
            if (checkTypeCompatibility(itemType) == false)
            {
                throw CliConfigurationException
                    .OptionCollectionItemTypeMismatch(
                        AliasPipeExpression,
                        _converter.GetType(),
                        itemType);
            }
        }
        else
        {
            _groupBinder = ToValue;
            _converter ??= System.ComponentModel.TypeDescriptor.GetConverter(OptionType);
            if (checkTypeCompatibility(OptionType) == false)
            {
                throw CliConfigurationException
                    .InvalidOptionTypeConverter(
                        AliasPipeExpression,
                        OptionType,
                        _converter.GetType());
            }
        }

        ThrowIf.NullReference(TypeDescriptor);
        ThrowIf.NullReference(_converter);

        Debug.Assert(_converter is not null);
        

        if (defaultValue is not null &&
            optionType.IsInstanceOfType(defaultValue) == false)
        {
            throw CliConfigurationException.DefaultValueTypeMismatch(AliasPipeExpression, OptionType, defaultValue);
        }

        if (_converter.CanConvertFrom(typeof(string)) == false)
        {
            throw CliConfigurationException.OptionTypeConversionFailure(AliasPipeExpression, OptionType);
        }



        ThrowIf.NullOrWhiteSpace(AliasPipeExpression);
        var pipeExp = AliasPipeExpression.Replace("?", "[?]");
        RegularExpression = TypeDescriptor.CreateRegularExpression(RegexMatchGroupName, pipeExp);
    }


    public static CliOptionInfo Create(
        ICliOptionMetadata metadata,
        string name,
        object? defaultValue,
        string description,
        Type optionType,
        bool isRequired)
    {
        var config = new Config
        {
            Name = name,
            Description = description,
            Metadata = metadata,
            IsRequired = isRequired,
            OptionType = optionType,
            DefaultValue = defaultValue
        };

        CliOptionInfo result;

        if (CliFlagOptionInfo.IsMatch(config, out result))
        {
            Debug.Assert(result is CliFlagOptionInfo);
        }
        else if (CliDictionaryOptionInfo.IsMatch(config, out result))
        {
            Debug.Assert(result is CliDictionaryOptionInfo);
        }
        else if (CliCollectionOptionInfo.IsMatch(config, out result))
        {
            Debug.Assert(result is CliCollectionOptionInfo);
        }
        else if (CliValueOptionInfo.IsMatch(config, out result))
        {
            Debug.Assert(result is CliValueOptionInfo);
        }
        else
        {
            throw new InvalidOperationException();
        }

        return ThrowIf.NullReference(result);
    }

    public abstract object Deserialize(Group optionGroup, CliTokenDecoder decoder);

    public ICliOptionMetadata OptionMetadata { get; }

    public string RegularExpression { get; }


    public bool IsRequired { get; }

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
            throw CliExitException.ConflictingOptionValues(AliasPipeExpression);
        }

        var input = decoder(group.Captures[0].Value);
        try
        {
            return _converter.ConvertFromInvariantString(input, descriptor.ValueType);
        }
        catch (Exception e) when (e is InvalidOperationException)
        {
            throw CliConfigurationException.InvalidOptionInputConversion(AliasPipeExpression, input, descriptor.ValueType);
        }
        catch (Exception e) when (e is FormatException or ArgumentException)
        {
            // Means the user supplied a wrong input text
            throw CliExitException.InvalidOptionInputParsing(AliasPipeExpression, descriptor.ValueType);
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
                    throw CliConfigurationException.InvalidCollectionOptionConversion(AliasPipeExpression, descriptor.ItemType);
                }
                catch (Exception e) when (e is FormatException or ArgumentException)
                {
                    throw CliExitException.CollectionOptionParsingFailure(AliasPipeExpression, descriptor.ItemType);
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
            CliDictionaryTypeDescriptor.IsMatch(capture.Value, out var keyGroup, out var valueGroup);
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
                    throw CliConfigurationException
                        .OptionValueConversionFailure(AliasPipeExpression, key, descriptor.ValueType);
                }
                catch (Exception e) when (e is FormatException or ArgumentException)
                {
                    throw CliExitException
                        .DictionaryOptionValueParseFailure(AliasPipeExpression, key, descriptor.ValueType);
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

}
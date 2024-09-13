using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System;
using System.ComponentModel;
using System.Linq;
using Solitons.Collections;
using Solitons.Caching;
namespace Solitons.CommandLine;

internal sealed record CliOptionInfo
{
    delegate object? GroupBinder(Group group, CliTokenDecoder decoder);

    private readonly object? _defaultValue;
    private readonly TypeConverter _converter;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly GroupBinder _groupBinder;

    public CliOptionInfo(
        ICliOptionMetadata metadata,
        string name,
        object? defaultValue,
        string description,
        Type optionType)
    {
        OptionMetadata = ThrowIf.ArgumentNull(metadata);
        OptionType = optionType = Nullable.GetUnderlyingType(optionType) ?? optionType;
        _defaultValue = defaultValue;
        Aliases = metadata.Aliases;
        Description = description;
        OptionType = optionType;
        RegexMatchGroupName = $"option_{name}_{Guid.NewGuid():N}";
        AliasPipeExpression = Aliases.Join("|");
        AliasCsvExpression = Aliases
            .OrderBy(alias => alias.StartsWith("--") ? 1 : 0)
            .ThenBy(alias => alias.Length)
            .Join(",");


        Func<Type, bool> checkTypeCompatibility = (type) => true;
        if (metadata.HasCustomTypeConverter(
                out var customConverter, 
                out var inputSample))
        {
            _converter = customConverter;
            try
            {
                var sampleValue = ThrowIf.NullReference(customConverter
                    .ConvertFromInvariantString(inputSample));
                checkTypeCompatibility = (type) => type.IsInstanceOfType(sampleValue);
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
            TypeDescriptor = new CliValueOptionTypeDescriptor(OptionType);
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
            throw new CliConfigurationException(
                $"The provided default value is not of type {optionType}. Actual type is {defaultValue.GetType()}");
        }

        if (_converter.CanConvertFrom(typeof(string)) == false)
        {
            throw new CliConfigurationException(
                $"The '{AliasPipeExpression}' option value tokens cannot be converted from a string to the specified option type '{OptionType}' using the default type converter. " +
                $"To resolve this, correct the option type if it's incorrect, or specify a custom type converter " +
                $"either by inheriting from '{typeof(CliOptionAttribute).FullName}' and overriding '{nameof(CliOptionAttribute.HasCustomTypeConverter)}()', " +
                $"or by applying the '{typeof(TypeConverterAttribute).FullName}' directly on the parameter or property.");
        }



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

}
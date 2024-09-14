using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using Solitons.Collections;

namespace Solitons.CommandLine;

internal sealed record CliCollectionOptionInfo : CliOptionInfo
{
    private readonly Type _collectionType;
    private readonly TypeConverter _elementTypeConverter;

    private static readonly IReadOnlyList<Type> SupportedGenericTypes = new[]
    {
        typeof(List<>),
        typeof(Stack<>),
        typeof(Queue<>),
        typeof(HashSet<>)
    }.AsReadOnly();

    private CliCollectionOptionInfo(
        Config config, 
        Type collectionType,
        Type elementType) 
        : base(config)
    {
        _collectionType = collectionType;
        ElementType = elementType;
        if (config.Metadata.CanAccept(elementType, out var elementTypeConverter) &&
            elementTypeConverter.CanConvertFrom(typeof(string)))
        {
            _elementTypeConverter = elementTypeConverter;
        }
        else
        {
            throw CliConfigurationException.CollectionElementParsingNotSupported();
        }
    }

    public Type ElementType { get; }

    public static bool IsMatch(Config config, out CliOptionInfo? result)
    {
        result = null;
        if (config.OptionType == typeof(string) ||
            config.OptionType == typeof(Unit) ||
            config.OptionType == typeof(CliFlag) ||
            config.OptionType.IsEnum ||
            typeof(IDictionary).IsAssignableFrom(config.OptionType) ||
            typeof(IEnumerable).IsAssignableFrom(config.OptionType) == false)
        {
            Debug.WriteLine($"{config.OptionType} is not a collection or an incompatible type.");
            return false;
        }


        if (config.OptionType.IsArray)
        {
            var elementType = config.OptionType.GetElementType()!;
            result = new CliCollectionOptionInfo(config, config.OptionType, elementType);
            return true;
        }

        if (config.OptionType.IsGenericType == false ||
            config.OptionType.GetGenericArguments().Length != 1)
        {
            return false;
        }

        {
            Debug.Assert(config.OptionType.IsGenericType);
            Debug.Assert(config.OptionType.GetGenericArguments().Length == 1);

            var elementType = config.OptionType.GetGenericArguments()[0];
            foreach (var genericType in SupportedGenericTypes)
            {
                var supportedType = genericType.MakeGenericType(elementType);
                if (config.OptionType.IsAssignableFrom(supportedType))
                {
                    result = new CliCollectionOptionInfo(config, supportedType, elementType);
                    return true;
                }
            }
        }

        return false;
    }

    public override object Deserialize(Group optionGroup, CliTokenDecoder decoder)
    {
        ThrowIf.ArgumentNull(optionGroup);
        ThrowIf.ArgumentNull(decoder);
        Debug.Assert(optionGroup.Success);
        var inputs = optionGroup.Captures.Select(c => c.Value);
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
                    var item = _elementTypeConverter.ConvertFromInvariantString(text, ElementType);
                    return item;
                }
                catch (Exception e) when (e is InvalidOperationException)
                {
                    throw CliConfigurationException.InvalidCollectionOptionConversion(AliasPipeExpression, ElementType);
                }
                catch (Exception e) when (e is FormatException or ArgumentException)
                {
                    throw CliExitException.CollectionOptionParsingFailure(AliasPipeExpression, ElementType);
                }
            })
            .ToList();

        var comparer = OptionMetadata.GetValueComparer();
        return CollectionBuilder.BuildCollection(_collectionType, items, comparer);
    }

    protected override string BuildOptionRegularExpression(string pipeExp) => $@"(?:{pipeExp})\s*(?<{RegexMatchGroupName}>(?:[^\s-]\S*)?)";
}
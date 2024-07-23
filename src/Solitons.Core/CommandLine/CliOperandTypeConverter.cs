using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal abstract class CliOperandTypeConverter
{
    protected CliOperandTypeConverter(Type type)
    {
        
    }

    public object FromMatch(Match match)
    {
        if (false == match.Success)
        {
            throw new ArgumentException();
        }

        return Convert(match);
    }

    protected abstract object Convert(Match match);

    public static CliOperandTypeConverter Create(Type type)
    {
        if (CliFlagOperandTypeConverter.IsFlag(type))
        {
            return new CliFlagOperandTypeConverter(type);
        }

        //TODO: if type is Dictionary<string, T> return CliMapOperandTypeConverter
        throw new NotFiniteNumberException();
    }
}

internal sealed class CliFlagOperandTypeConverter : CliOperandTypeConverter
{
    private readonly Type _type;

    private static readonly Dictionary<Type, object> SupportedTypes = new()
    {
        [typeof(Unit)] = Unit.Default
    };

    public CliFlagOperandTypeConverter(Type type) 
        : base(type)
    {
        if (false == SupportedTypes.ContainsKey(type))
        {
            throw new ArgumentOutOfRangeException();
        }

        _type = type;
    }

    public static bool IsFlag(Type type) => SupportedTypes.ContainsKey(type);


    protected override object Convert(Match _)
    {
        return SupportedTypes[_type];
    }
}

/// <summary>
/// Converts CLI token to the target type. 
/// </summary>
internal sealed class CliMapOperandTypeConverter : CliOperandTypeConverter
{
    private readonly Type _type;
    private readonly string _keyGroup;
    private readonly string _valueGroup;
    private readonly TypeConverter _valueTypeConverter;

    public CliMapOperandTypeConverter(
        Type type,
        string optionName)
        : base(type)
    {
        _type = type;
        ValueType = type.GetGenericArguments()[1];
        _valueTypeConverter = TypeDescriptor.GetConverter(ValueType);
        if (!_valueTypeConverter.CanConvertFrom(typeof(string)))
        {
            throw new InvalidOperationException(
                $"Cannot convert from string to {ValueType}");
        }
        _keyGroup = $"{optionName}_KEY";
        _valueGroup = $"{optionName}_VALUE";
    }

    public static bool IsMap(Type type)
    {
        if (!type.IsGenericType) return false;

        var genericTypeDefinition = type.GetGenericTypeDefinition();
        return genericTypeDefinition == typeof(Dictionary<,>) &&
               type.GetGenericArguments()[0] == typeof(string);
    }

    public Type ValueType { get; }

    protected override object Convert(Match match)
    {
        var keys = match.Groups[_keyGroup].Captures;
        var values = match.Groups[_valueGroup].Captures;
        if (keys.Count != values.Count)
        {
            throw new InvalidOperationException();
        }

        var dictionaryType = typeof(Dictionary<,>).MakeGenericType(typeof(string), ValueType);
        var result = (IDictionary)Activator.CreateInstance(dictionaryType)!;

        for (int i = 0; i < keys.Count; ++i)
        {
            var key = keys[i].Value;
            var value = ConvertValue(values[i].Value);
            result.Add(key, value);
        }

        return result;
    }

    private object ConvertValue(string value)
    {
        return _valueTypeConverter.ConvertFromInvariantString(value);
    }
}
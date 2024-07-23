using System;
using System.Collections.Generic;
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

    public CliMapOperandTypeConverter(
        Type type, 
        string optionName) 
        : base(type)
    {
        _type = type;
        //TODO: initialize ValueType to be the dictionary value type
        _keyGroup = $"{optionName}_KEY";
        _valueGroup = $"{optionName}_VALUE";
    }

    public static bool IsMap(Type type)
    {
        //TODO: return true if the type is Dictionary<string, T>
        // or IDictionary<string, T>
        throw new NotImplementedException();
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

        //TODO: Create the resulting Dictionary object
        for (int i = 0; i < keys.Count; ++i)
        {
            var key = keys[i];
            var value = values[i];
            //TODO: Parse the value to the ValueType 
            // add the key value pair to the resulting collection
        }
        throw new NotImplementedException();
    }
}
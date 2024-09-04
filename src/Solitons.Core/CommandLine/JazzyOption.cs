using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System;
using System.Collections;
using System.ComponentModel;

namespace Solitons.CommandLine;

internal sealed record JazzyOption
{
    private readonly object? _defaultValue;
    private readonly string _expression;
    private readonly TypeConverter _converter;
    private readonly Func<Group, object?> _binder;

    public JazzyOption(
        CliOptionAttribute metadata,
        object? defaultValue,
        string description,
        Type valueType)
    {
        if (defaultValue is not null && 
            valueType.IsAssignableFrom(valueType) == false)
        {
            throw new InvalidOperationException("Oops...");
        }

        Metadata = metadata;
        _defaultValue = defaultValue;
        Aliases = metadata.Aliases;
        Description = description;
        ValueType = valueType;
        RegexMatchGroupName = $"option_{Guid.NewGuid():N}";
        _expression = Aliases.Join("|");
        Arity = CliUtils.GetOptionArity(valueType);
        _binder = Arity switch
        {
            (CliOptionArity.Map) => ToDictionary,
            (CliOptionArity.Vector) => ToCollection,
            (CliOptionArity.Scalar) => ToScalar,
            (CliOptionArity.Flag) => ToFlag,
            _ => throw new InvalidOperationException()
        };
    }

    private object? ToFlag(Group arg)
    {
        throw new NotImplementedException();
    }

    private object? ToScalar(Group arg)
    {
        throw new NotImplementedException();
    }

    public CliOptionAttribute Metadata { get; }

    public CliOptionArity Arity { get; }

    public required bool IsRequired { get; init; }

    public required CliTokenSubstitutionPreprocessor Preprocessor { get; init; }



    private string RegexMatchGroupName { get; }
    public IReadOnlyList<string> Aliases { get; }
    public string Description { get; }
    public Type ValueType { get; }

    public object? Bind(Match commandLineMatch)
    {
        Debug.Assert(commandLineMatch.Success);
        var group = commandLineMatch.Groups[RegexMatchGroupName];
        if (group.Success)
        {
            return _binder.Invoke(group);
        }

        if (IsRequired)
        {
            CliExit.With($"{_expression} option is required.");
        }

        return _defaultValue;

    }

    private object? ToCollection(Group group)
    {
        throw new NotImplementedException();
    }

    private object? ToDictionary(Group group)
    {
        var dictionaryType = typeof(Dictionary<,>).MakeGenericType(typeof(string), ValueType);
        var dictionary = (IDictionary)Activator.CreateInstance(dictionaryType, Metadata.GetMapKeyComparer())!;
        foreach (Capture capture in group.Captures)
        {
            var input = Regex.Replace(capture.Value, @"\[|\]", String.Empty);
            var match = Regex.Match(input, @"(?:\[$key\]\s+$value)|(?:$key\s+$value)"
                .Replace("$key", @"(?<key>\S+)?")
                .Replace("$value", @"(?<value>\S+)?"));
            var keyGroup = match.Groups["key"];
            var valueGroup = match.Groups["value"];
            if (keyGroup.Success && valueGroup.Success)
            {
                var value = _converter.ConvertFromInvariantString(valueGroup.Value);
                if (value is null)
                {
                    CliExit.With("Invalid value...");
                }
                dictionary[keyGroup.Value] = value;
            }
            else if(keyGroup.Success == valueGroup.Success)
            {
                CliExit.With("Key value pair is required");
            }
            else if(keyGroup.Success == false)
            {
                CliExit.With("Key is required");
            }
            else if (valueGroup.Success == false)
            {
                CliExit.With("Value is required");
            }
        }

        return dictionary;
    }
}
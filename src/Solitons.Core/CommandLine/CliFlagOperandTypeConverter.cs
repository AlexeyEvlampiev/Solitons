using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal sealed class CliFlagOperandTypeConverter : CliOperandTypeConverter
{
    private readonly Type _type;
    private readonly string _parameterName;

    private static readonly Dictionary<Type, object> SupportedTypes = new()
    {
        [typeof(Unit)] = Unit.Default,
        [typeof(Unit?)] = Unit.Default,
    };

    public CliFlagOperandTypeConverter(Type type, string parameterName) 
        : base(false)
    {
        if (false == SupportedTypes.ContainsKey(type))
        {
            throw new ArgumentOutOfRangeException();
        }

        _type = type;
        _parameterName = parameterName;
    }

    public static bool IsFlag(Type type) => SupportedTypes.ContainsKey(type);


    protected override object Convert(Match match, CliTokenDecoder decoder)
    {
        return SupportedTypes[_type];
    }

    public override string ToMatchPattern(string keyPattern)
    {
        return $"(?<{_parameterName}>{keyPattern})";
    }
}
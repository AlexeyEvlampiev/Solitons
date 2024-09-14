using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reactive;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal sealed record CliFlagOptionInfo : CliOptionInfo
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly TypeConverter _converter;

    private CliFlagOptionInfo(Config config) : base(config)
    {
        _converter = 
            config.OptionType == typeof(CliFlag) ? new CliFlagConverter() : 
            config.OptionType == typeof(Unit) ? new UnitConverter() : 
            throw new InvalidOperationException();
    }


    internal static bool IsMatch(Config config, out CliOptionInfo? result)
    {
        result = null;
        if (config.OptionType == typeof(CliFlag) ||
            config.OptionType == typeof(Unit))
        {
            result = new CliFlagOptionInfo(config);
            return true;
        }

        return false;
    }


    public override object Deserialize(Group optionGroup, CliTokenDecoder decoder)
    {
        Debug.Assert(optionGroup.Success);
        return
            _converter.ConvertFromInvariantString(optionGroup.Value)
            ?? throw new InvalidOperationException();
    }

    protected override string BuildOptionRegularExpression(string pipeExp) => $@"(?<{RegexMatchGroupName}>{pipeExp})";
}
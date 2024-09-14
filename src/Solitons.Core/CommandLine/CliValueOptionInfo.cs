using System.ComponentModel;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal sealed record CliValueOptionInfo : CliOptionInfo
{
    private readonly Type _type;
    private readonly TypeConverter _converter;

    private CliValueOptionInfo(Config config) : base(config)
    {
        _type = config.OptionType;
        _converter = config.Metadata.CanAccept(_type, out var converter) && converter.CanConvertFrom(typeof(string))
            ? converter
            : throw new InvalidOperationException();
    }

    public static bool IsMatch(Config config, out CliOptionInfo? result)
    {
        if (config.Metadata.CanAccept(config.OptionType, out var converter) &&
            converter.CanConvertFrom(typeof(string)))
        {
            result = new CliValueOptionInfo(config);
            return true;
        }

        result = null;
        return false;
    }

    public override object Deserialize(Group optionGroup, CliTokenDecoder decoder)
    {
        ThrowIf.ArgumentNull(optionGroup);
        ThrowIf.ArgumentNull(decoder);
        ThrowIf.False(optionGroup.Success);

        if (optionGroup.Captures.Count > 1 &&
            optionGroup.Captures
                .Select(c => c.Value)
                .Distinct(StringComparer.Ordinal)
                .Count() > 1)
        {
            throw CliExitException.ConflictingOptionValues(AliasPipeExpression);
        }

        var input = decoder(optionGroup.Captures[0].Value);
        try
        {
            return _converter.ConvertFromInvariantString(input, _type);
        }
        catch (Exception e) when (e is InvalidOperationException)
        {
            throw CliConfigurationException.InvalidOptionInputConversion(AliasPipeExpression, input, _type);
        }
        catch (Exception e) when (e is FormatException or ArgumentException)
        {
            // Means the user supplied a wrong input text
            throw CliExitException.InvalidOptionInputParsing(AliasPipeExpression, _type);
        }
    }

    protected override string BuildOptionRegularExpression(string pipeExp) =>
        $@"(?:{pipeExp})\s*(?<{RegexMatchGroupName}>(?:[^\s-]\S*)?)";
}
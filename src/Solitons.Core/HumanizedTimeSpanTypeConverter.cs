using System;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Solitons;

public sealed class HumanizedTimeSpanTypeConverter : TypeConverter
{
    private static readonly Regex TimeSpanRegex;
    private static readonly Regex FractionRegex;

    static HumanizedTimeSpanTypeConverter()
    {
        TimeSpanRegex = new Regex(
            @"^$pattern(?:\s+$pattern)*$"
                .Replace("$pattern", @"(?<fraction>[\d\.]+\s+\w+)"));
        FractionRegex = new Regex(
            @"(?xim-s)$value \s+ $units"
                .Replace("$value", @"(?<value>(?:\.\d+|\d+(?:\.\d*)?))")
                .Replace("$units", @"(?<units>\w+)"));
    }


    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is string input)
        {
            return Parse(input);
        }
        return base.ConvertFrom(context, culture, value);
    }

    private static TimeSpan? Parse(string input)
    {
        var timespanMatch = ThrowIf
            .ArgumentNullOrWhiteSpace(input)
            .Trim('\'', '\"')
            .ToLowerInvariant()
            .Convert(s => Regex.Replace(s, @"\b(?:seconds?|secs?)\b", "seconds"))
            .Convert(s => Regex.Replace(s, @"\bminutes?|mins?\b", "minutes"))
            .Convert(s => Regex.Replace(s, @"\bhours?|hrs?\b", "hours"))
            .Convert(s => Regex.Replace(s, @"\bdays?\b", "days"))
            .Convert(s => TimeSpanRegex.Match(s));

        if (false == timespanMatch.Success) 
        {
            throw new FormatException();
        }

        TimeSpan result = TimeSpan.Zero;
        foreach (Capture capture in timespanMatch.Groups["fraction"].Captures) 
        { 
            var fractionMatch = FractionRegex.Match(capture.Value);
            if (!fractionMatch.Success) 
            { 
                throw new FormatException();
            }
            var valueText = fractionMatch.Groups["value"].Value;
            var units = fractionMatch.Groups["units"].Value;

            double value = double.Parse(fractionMatch.Groups["value"].Value, CultureInfo.InvariantCulture);
            result += units switch
            {
                "seconds" => TimeSpan.FromSeconds(value),
                "minutes" => TimeSpan.FromMinutes(value),
                "hours" => TimeSpan.FromHours(value),
                "days" => TimeSpan.FromDays(value),
                _ => throw new FormatException($"Unrecognized time unit: {units}")
            };
  
        }

        return result;
    }

    public static bool TryParse(string text, out TimeSpan timeout)
    {
        timeout = TimeSpan.Zero;
        if (TimeSpanRegex.IsMatch(text))
        {
            try
            {
                timeout = Parse(text) ?? throw new FormatException();
                return true;
            }
            catch (FormatException e)
            {
                return false;
            }
        }

        return false;
    }
}
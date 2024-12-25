using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Solitons;

/// <summary>
/// Provides a type converter to convert <see cref="TimeSpan"/> objects to and from their human-readable string representations.
/// </summary>
public sealed class HumanReadableTimeSpanConverter : TypeConverter
{
    private static readonly Regex TimeSpanRegex;
    private static readonly Regex FractionRegex;

    /// <summary>
    /// Initializes the <see cref="HumanReadableTimeSpanConverter"/> class by defining the regular expressions used for parsing.
    /// </summary>
    static HumanReadableTimeSpanConverter()
    {
        TimeSpanRegex = new Regex(
            @"^$pattern(?:\s+$pattern)*$"
                .Replace("$pattern", @"(?<fraction>[\d\.]+\s+\w+)"));
        FractionRegex = new Regex(
            @"(?xim-s)$value \s+ $units"
                .Replace("$value", @"(?<value>(?:\.\d+|\d+(?:\.\d*)?))")
                .Replace("$units", @"(?<units>\w+)"));
    }


    /// <inheritdoc />
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    /// <inheritdoc />
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string input)
        {
            return Parse(input);
        }
        return base.ConvertFrom(context, culture, value);
    }

    /// <summary>
    /// Parses a human-readable string representation of a <see cref="TimeSpan"/> into a <see cref="TimeSpan"/> object.
    /// </summary>
    /// <param name="input">The input string to parse.</param>
    /// <returns>A <see cref="TimeSpan"/> object that corresponds to the parsed input string.</returns>
    /// <exception cref="FormatException">Thrown when the input string does not match the expected time span format.</exception>
    private static TimeSpan? Parse(string input)
    {
        input = ThrowIf.ArgumentNullOrEmpty(input, "Input string cannot be null, empty, or consist only of white-space characters.");
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
            throw new FormatException($"The input string '{input}' does not match the expected time span format.");
        }

        TimeSpan result = TimeSpan.Zero;
        foreach (Capture capture in timespanMatch.Groups["fraction"].Captures) 
        { 
            var fractionMatch = FractionRegex.Match(capture.Value);
            if (!fractionMatch.Success) 
            {
                throw new FormatException($"The time span fraction '{capture.Value}' is not in a recognized format.");
            }
            var valueText = fractionMatch.Groups["value"].Value;
            var units = fractionMatch.Groups["units"].Value;

            if (!double.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            {
                throw new FormatException($"The value '{valueText}' is not a valid number.");
            }
            result += units switch
            {
                "seconds" => TimeSpan.FromSeconds(value),
                "minutes" => TimeSpan.FromMinutes(value),
                "hours" => TimeSpan.FromHours(value),
                "days" => TimeSpan.FromDays(value),
                _ => throw new FormatException($"Unrecognized time unit: '{units}'. Valid units are 'seconds', 'minutes', 'hours', and 'days'.")
            };
  
        }

        return result;
    }

    /// <summary>
    /// Attempts to parse a human-readable string representation of a <see cref="TimeSpan"/> into a <see cref="TimeSpan"/> object.
    /// </summary>
    /// <param name="text">The input string to parse.</param>
    /// <param name="timeout">When this method returns, contains the parsed <see cref="TimeSpan"/> value, or <see cref="TimeSpan.Zero"/> if parsing fails.</param>
    /// <returns><see langword="true"/> if parsing was successful; otherwise, <see langword="false"/>.</returns>
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
                Debug.WriteLine(e.Message);
                return false;
            }
        }

        return false;
    }
}
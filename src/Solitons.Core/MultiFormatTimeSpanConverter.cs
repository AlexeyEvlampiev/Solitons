using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Xml;

namespace Solitons;

/// <summary>
/// Provides a type converter to convert <see cref="TimeSpan"/> objects from various string formats, including .NET, human-readable, and ISO 8601 duration formats.
/// </summary>
public sealed class MultiFormatTimeSpanConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string);
    }

    /// <inheritdoc />
    [DebuggerStepThrough]
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        ThrowIf.ArgumentNull(value, "Value cannot be null.");
        if (value is string text)
        {
            return Parse(text);
        }

        throw new ArgumentException("The provided value must be a string representing a TimeSpan.", nameof(value));
    }

    /// <summary>
    /// Parses a string representation of a <see cref="TimeSpan"/> from various formats, including .NET, human-readable, and ISO 8601 duration formats.
    /// </summary>
    /// <param name="timeoutText">The string representation of the time span to parse.</param>
    /// <returns>A <see cref="TimeSpan"/> object that represents the parsed value.</returns>
    /// <exception cref="ArgumentException">Thrown when the input string is null or empty.</exception>
    /// <exception cref="FormatException">Thrown when the input string is not in a valid time span format.</exception>
    private object? Parse(string timeoutText)
    {
        ThrowIf.ArgumentNullOrWhiteSpace(timeoutText, "Timeout text cannot be null or empty");

        try
        {
            if (TimeSpan.TryParse(timeoutText, out var timeout) ||
                HumanReadableTimeSpanConverter.TryParse(timeoutText, out timeout))
            {
                // .NET TimeSpan format
                return timeout;
            }


            // ISO 8601 duration format
            timeout = XmlConvert.ToTimeSpan(timeoutText);
            return timeout;
        }
        catch (FormatException ex)
        {
            throw new FormatException($"Invalid timeout format: {timeoutText}", ex);
        }
    }
}
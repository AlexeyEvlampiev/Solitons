using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Xml;

namespace Solitons.CommandLine;

public sealed class CliCancellationTokenTypeConverter : TypeConverter
{
    [DebuggerStepThrough]
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        return Parse(value.ToString() ?? throw new InvalidOperationException("Invalid timeout text"));
    }

    private object? Parse(string timeoutText)
    {
        if (string.IsNullOrWhiteSpace(timeoutText))
        {
            throw new ArgumentException("Timeout text cannot be null or empty", nameof(timeoutText));
        }

        try
        {
            if (TimeSpan.TryParse(timeoutText, out var timeout) ||
                HumanizedTimeSpanTypeConverter.TryParse(timeoutText, out timeout))
            {
                // .NET TimeSpan format
                return new CancellationTokenSource(timeout).Token;
            }


            // ISO 8601 duration format
            timeout = XmlConvert.ToTimeSpan(timeoutText);
            return new CancellationTokenSource(timeout).Token;
        }
        catch (FormatException ex)
        {
            throw new FormatException($"Invalid timeout format: {timeoutText}", ex);
        }
    }
}
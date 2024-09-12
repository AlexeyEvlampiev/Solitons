using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reactive;

namespace Solitons.CommandLine;

internal sealed class CliFlagConverter : TypeConverter
{
    private readonly Dictionary<Type, object> _flagTypes = new()
    {
        [typeof(CliFlag)] = CliFlag.Default
    };


    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string);
    }


    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (_flagTypes.TryGetValue(destinationType, out value))
        {
            return value;
        }

        throw new InvalidOperationException();
    }

    // Override ConvertFrom to handle conversion from string to specific types
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string)
        {
            return CliFlag.Default;
        }

        throw new InvalidOperationException($"Cannot convert from {value.GetType()}");
    }
}


internal sealed class UnitConverter : TypeConverter
{
    private readonly Dictionary<Type, object> _flagTypes = new()
    {
        [typeof(Unit)] = Unit.Default
    };


    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string);
    }


    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (_flagTypes.TryGetValue(destinationType, out value))
        {
            return value;
        }

        throw new InvalidOperationException();
    }

    // Override ConvertFrom to handle conversion from string to specific types
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string)
        {
            return Unit.Default;
        }

        throw new InvalidOperationException($"Cannot convert from {value.GetType()}");
    }
}
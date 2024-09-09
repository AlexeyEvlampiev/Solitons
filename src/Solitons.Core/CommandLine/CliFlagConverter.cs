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
        [typeof(Unit)] = Unit.Default
    };

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        return destinationType != null && _flagTypes.ContainsKey(destinationType);
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (_flagTypes.TryGetValue(destinationType, out value))
        {
            return value;
        }

        throw new InvalidOperationException();
    }
}
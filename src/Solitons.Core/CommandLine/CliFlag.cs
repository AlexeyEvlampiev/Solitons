using System;
using System.ComponentModel;
using System.Globalization;
using System.Reactive;

namespace Solitons.CommandLine;

public sealed class CliFlag
{
    public static readonly CliFlag Default = new();

    public static implicit operator Unit(CliFlag flag) => Unit.Default;

    public static implicit operator CliFlag(Unit unit) => Default;


    internal static bool IsFlagType(Type type) => type == typeof(CliFlag) || type == typeof(Unit);


    internal static bool IsFlagType(Type type, out TypeConverter? valueConverter)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        if (underlyingType == typeof(CliFlag))
        {
            valueConverter = new CliFlagTypeConverter();
            return true;
        }

        if (underlyingType == typeof(Unit))
        {
            valueConverter = new UnitTypeConverter();
            return true;
        }

        valueConverter = null;
        return false;
    }


    sealed class CliFlagTypeConverter : TypeConverter
    {
        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            return CliFlag.Default;
        }
    }

    sealed class UnitTypeConverter : TypeConverter
    {
        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            return Unit.Default;
        }
    }

}
﻿using System;
using System.Reactive;

namespace Solitons.CommandLine;

public sealed class CliFlag
{
    public static readonly CliFlag Default = new();

    public static implicit operator Unit(CliFlag flag) => Unit.Default;

    public static implicit operator CliFlag(Unit unit) => Default;


    internal static bool IsFlagType(Type type, out object defaultValue)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        defaultValue = Default;
        if (underlyingType == typeof(CliFlag))
        {
            defaultValue = Default;
            return true;
        }

        if (underlyingType == typeof(Unit))
        {
            defaultValue = Unit.Default;
            return true;
        }
        return false;
    }
}
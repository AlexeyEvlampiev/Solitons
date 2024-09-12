﻿using System.Reactive;

namespace Solitons.CommandLine;

public sealed class CliFlag
{
    public static readonly CliFlag Default = new();

    public static implicit operator Unit(CliFlag flag) => Unit.Default;

    public static implicit operator CliFlag(Unit unit) => Default;
}
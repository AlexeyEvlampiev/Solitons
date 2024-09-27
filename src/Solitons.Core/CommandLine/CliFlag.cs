using System;
using System.Reactive;

namespace Solitons.CommandLine;

public sealed class CliFlag
{
    public static readonly CliFlag Default = new();

    public static implicit operator Unit(CliFlag flag) => Unit.Default;

    public static implicit operator CliFlag(Unit unit) => Default;


    internal static bool IsFlagType(Type type) => type == typeof(CliFlag) || type == typeof(Unit);
}
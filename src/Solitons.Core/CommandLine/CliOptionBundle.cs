using System;

namespace Solitons.CommandLine;

public abstract class CliOptionBundle
{
    public static bool IsAssignableFrom(Type type) => typeof(CliOptionBundle).IsAssignableFrom(type);

}
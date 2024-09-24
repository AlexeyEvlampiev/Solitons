using System;
using System.Diagnostics;
using System.Reflection;

namespace Solitons.CommandLine;

public interface ICliConfigOptions
{
    ICliConfigOptions UseCommandsFrom(
        object program,
        string baseRoute = "",
        BindingFlags binding = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

    ICliConfigOptions UseCommandsFrom(
        Type declaringType,
        string baseRoute = "",
        BindingFlags binding = BindingFlags.Static | BindingFlags.Public);

    ICliConfigOptions UseLogo(string logo);

    ICliConfigOptions UseDescription(string description);


    [DebuggerStepThrough]
    public sealed ICliConfigOptions UseCommandsFrom<T>(
        string baseRoute = "",
        BindingFlags binding = BindingFlags.Static | BindingFlags.Public) =>
        UseCommandsFrom(typeof(T), baseRoute, binding);
}
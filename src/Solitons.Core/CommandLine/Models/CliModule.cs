using System;
using System.Reflection;

namespace Solitons.CommandLine.Models;

internal sealed record CliModule
{
    public CliModule(
        Type programType, 
        BindingFlags binding = BindingFlags.Public | BindingFlags.Static, 
        string baseRoute = "")
    {
        Program = binding.HasFlag(BindingFlags.Instance) 
            ? Activator.CreateInstance(programType) ?? throw new InvalidOperationException("Oops...")
            : null;
        ProgramType = programType;
        Binding = binding;
        BaseRoute = baseRoute;
    }

    public CliModule(
        object program, 
        BindingFlags binding = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, 
        string baseRoute = "")
    {
        Program = program;
        ProgramType = program.GetType();
        Binding = binding | BindingFlags.Instance;
        BaseRoute = baseRoute;
    }

    public object? Program { get; init; }

    public Type ProgramType { get; init; }
    public BindingFlags Binding { get; init; }
    public string BaseRoute { get; init; }

}
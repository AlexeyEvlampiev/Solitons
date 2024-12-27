using System;

namespace Solitons.CommandLine.Reflection;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class CliCommandExampleAttribute(string example, string description = "") : Attribute
{
    public string Example { get; } = example;
    public string Description { get; } = description;

    public override string ToString() => Example;
}
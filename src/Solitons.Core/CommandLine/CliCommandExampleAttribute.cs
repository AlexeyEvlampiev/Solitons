using System;

namespace Solitons.CommandLine;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class CliCommandExampleAttribute : Attribute
{
    public CliCommandExampleAttribute(string example)
    {
        Example = example;
    }

    public string Example { get; }
}
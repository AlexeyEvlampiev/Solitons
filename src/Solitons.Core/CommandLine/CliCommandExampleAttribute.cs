using System;

namespace Solitons.CommandLine;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class CliCommandExampleAttribute : Attribute
{
    public CliCommandExampleAttribute(string example, string description = "")
    {
        Example = example;
        Description = description;
    }

    public string Example { get; }
    public string Description { get; }
}
using System;

namespace Solitons.CommandLine;

/// <summary>
/// Defines a CLI command that can be composed of multiple subcommands.
/// This attribute should be applied to methods that represent complex CLI commands,
/// where each part of the command string represents a distinct subcommand.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class CliRouteAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the CliCommandAttribute class.
    /// </summary>
    /// <param name="routeDeclaration">A space-separated string representing individual subcommands.</param>
    public CliRouteAttribute(string routeDeclaration)
    {
        RouteDeclaration = routeDeclaration;
    }

    /// <summary>
    /// Gets a space-separated string representing individual subcommands.
    /// </summary>
    public string RouteDeclaration { get; }



    public override string ToString() => RouteDeclaration;
}
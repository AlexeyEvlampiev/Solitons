using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine.Reflection;

/// <summary>
/// Defines a CLI command that can be composed of multiple subcommands.
/// This attribute should be applied to methods that represent complex CLI commands,
/// where each part of the command string represents a distinct subcommand.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class CliRouteAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the CliCommandAttribute class.
    /// </summary>
    /// <param name="routeSignature">A space-separated string representing individual subcommands.</param>
    public CliRouteAttribute(string routeSignature)
    {
        RouteSignature = routeSignature;
        Segments = [
            ..Regex
                .Split(RouteSignature, @"(?<=\S)\s+(?=\S)")
                .Select(segment => segment.Trim())
        ];
    }

    /// <summary>
    /// Gets a space-separated string representing individual subcommands.
    /// </summary>
    public string RouteSignature { get; }

    public ImmutableArray<string> Segments { get; }

    public override string ToString() => RouteSignature;
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

/// <summary>
/// Defines a CLI command that can be composed of multiple subcommands.
/// This attribute should be applied to methods that represent complex CLI commands,
/// where each part of the command string represents a distinct subcommand.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class CliRouteAttribute : Attribute, IEnumerable<CliSubCommandInfo>
{

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    private readonly CliSubCommandInfo[] _subCommands;

    /// <summary>
    /// Initializes a new instance of the CliCommandAttribute class.
    /// </summary>
    /// <param name="routeExpression">A space-separated string representing individual subcommands.</param>
    public CliRouteAttribute(string routeExpression)
    {
        RouteExpression = routeExpression;
        _subCommands = routeExpression
            .DefaultIfNullOrWhiteSpace(string.Empty)
            .Trim()
            .Convert(cmd => Regex.Split(cmd, @"\s+"))
            .Select(segment => new CliSubCommandInfo(segment))
            .ToArray();
    }

    /// <summary>
    /// Gets a space-separated string representing individual subcommands.
    /// </summary>
    public string RouteExpression { get; }

    [DebuggerNonUserCode]
    IEnumerator<CliSubCommandInfo> IEnumerable<CliSubCommandInfo>.GetEnumerator()
    {
        foreach (var cmdlet in _subCommands)
        {
            yield return cmdlet;
        }
    }

    [DebuggerNonUserCode]
    IEnumerator IEnumerable.GetEnumerator() => _subCommands.GetEnumerator();

    public override string ToString() => RouteExpression;
}
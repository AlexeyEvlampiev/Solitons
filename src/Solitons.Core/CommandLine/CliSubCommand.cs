using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using System.Linq;

namespace Solitons.CommandLine;

/// <summary>
/// Represents a single CLI subcommand, encapsulating its aliases and providing pattern matching functionality.
/// </summary>
internal sealed class CliSubCommandInfo : ICliRouteCommandSegmentMetadata
{
    private static readonly Regex ValidCommandOptionsRegex;
    private static readonly Regex ValidCommandSegmentRegex = new(@"^(?:\w+(?:-\w+)*)?$");

    internal const string ArgumentExceptionMessage =
        "Command pattern format is invalid. Ensure it is a pipe-separated list of valid command names.";



    static CliSubCommandInfo()
    {
        var pattern = @"^(?:$cmd(?:\|$cmd)*)?$"
            .Replace("$cmd", @"\w+(?:-\w+)*");
        ValidCommandOptionsRegex = new Regex(pattern);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CliSubCommandInfo"/> class with a specific command pattern.
    /// </summary>
    /// <param name="commandPattern">A pipe-separated string containing different aliases for the command.</param>
    /// <exception cref="ArgumentException">Thrown when any alias in the pattern does not match the allowed command name format.</exception>
    public CliSubCommandInfo(string commandPattern)
    {
        if (!ValidCommandOptionsRegex.IsMatch(commandPattern) &&
            !commandPattern.IsNullOrWhiteSpace())
        {
            throw new ArgumentException(ArgumentExceptionMessage);
        }

        Aliases = commandPattern
            .DefaultIfNullOrEmpty(String.Empty)
            .Split("|")
            .Select(o => o.Trim())
            .Distinct()
            .OrderBy(o => o, StringComparer.Ordinal)
            .ToArray();

        foreach (var name in Aliases)
        {
            if (!ValidCommandSegmentRegex.IsMatch(name) &&
                !name.IsNullOrWhiteSpace())
                throw new ArgumentException($"Invalid CLI command name: {name}");
        }

        SubCommandPattern = Aliases
            .Join("|")
            .DefaultIfNullOrEmpty("");
    }



    /// <summary>
    /// Gets the regular expression pattern derived from all aliases, used to match the subcommand in a CLI context.
    /// </summary>
    public string SubCommandPattern { get; }

    /// <summary>
    /// Gets a read-only list of all aliases for the subcommand.
    /// </summary>
    public IReadOnlyList<string> Aliases { get; }

    public override string ToString() => SubCommandPattern;

    public string BuildRegularExpression() => SubCommandPattern;
}
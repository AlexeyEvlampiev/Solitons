using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Solitons.Text.RegularExpressions;

namespace Solitons.CommandLine.Models;

/// <summary>
/// Represents a model for a CLI subcommand route that processes and validates aliases.
/// </summary>
internal sealed record CliRouteSubcommandModel : ICliCommandSegmentModel
{
    private const string AliasPattern = @"\w+"; 
    private const string PipeDelimiter = "|";
    private const string CommaDelimiter = ",";

    private static readonly Regex ValidAliasesPsvRegex = new(
        @$"^{AliasPattern}(?:\{PipeDelimiter}{AliasPattern})*$",
        RegexOptions.Compiled | 
        RegexOptions.CultureInvariant);


    private CliRouteSubcommandModel(string pipeSeparatedAliases)
    {
        pipeSeparatedAliases = ThrowIf
            .ArgumentNullOrWhiteSpace(pipeSeparatedAliases)
            .Convert(RegexUtils.RemoveWhitespace);

        if (false == ValidAliasesPsvRegex.IsMatch(pipeSeparatedAliases))
        {
            throw new ArgumentException(nameof(pipeSeparatedAliases), 
                $"Invalid alias format: '{pipeSeparatedAliases}' must be pipe-separated words.");
        }

        var sortedAliases = pipeSeparatedAliases
            .Split(PipeDelimiter, StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(alias => alias.Length)
            .ThenBy(alias => alias, StringComparer.OrdinalIgnoreCase)
            .ToImmutableArray();

        if (sortedAliases.Length != pipeSeparatedAliases.Split(PipeDelimiter).Length)
        {
            throw new ArgumentException($"Duplicate aliases found: {string.Join(CommaDelimiter, sortedAliases)}", nameof(pipeSeparatedAliases));
        }

        Aliases = sortedAliases;
        PipeDelimitedAliases = string.Join(PipeDelimiter, sortedAliases);
        RegexPattern = $"(?:{PipeDelimitedAliases})";
        Synopsis = sortedAliases
            .OrderBy(a => a.Length)
            .ThenBy(a => a, StringComparer.OrdinalIgnoreCase)
            .Join(PipeDelimiter);

        ThrowIf.False(Aliases.Any());
        ThrowIf.False(PipeDelimitedAliases.IsPrintable());
        ThrowIf.False(Synopsis.IsPrintable());
        ThrowIf.False(RegexPattern.IsPrintable());
    }

    /// <summary>
    /// Creates an array of <see cref="CliRouteSubcommandModel"/> from a route string.
    /// </summary>
    /// <param name="route">The route string to parse.</param>
    /// <returns>An array of parsed <see cref="CliRouteSubcommandModel"/> instances.</returns>
    public static CliRouteSubcommandModel[] FromRoute(string route)
    {
        return Regex
            .Split(route, @"(?<=[^|])\s+(?=[^|])")
            .Select(RegexUtils.RemoveWhitespace)
            .Where(s => s.IsPrintable())
            .Select(s => new CliRouteSubcommandModel(s))
            .ToArray();
    }

    /// <summary>
    /// Gets the pipe-separated alias values.
    /// </summary>
    public string PipeDelimitedAliases { get; }


    /// <summary>
    /// Gets the regex pattern representing the aliases.
    /// </summary>
    public string RegexPattern { get; }

    public string Synopsis { get; }

    /// <summary>
    /// Converts the aliases to a CSV format.
    /// </summary>
    /// <param name="includeSpaceAfterComma">If true, adds a space after each comma in the CSV.</param>
    /// <returns>A CSV string representation of the aliases.</returns>
    public string ToCsv(bool includeSpaceAfterComma = false)
    {
        return Aliases
            .OrderBy(a => a.Length)
            .ThenBy(a => a)
            .Join(includeSpaceAfterComma ? ", " : ",");
    }

    /// <summary>
    /// Gets the immutable array of aliases.
    /// </summary>
    public ImmutableArray<string> Aliases { get; }

    public override string ToString() => ToCsv(includeSpaceAfterComma: true);
    string ICliCommandSegmentModel.ToSynopsis() => Synopsis;
}

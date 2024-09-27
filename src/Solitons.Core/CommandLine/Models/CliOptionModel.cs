using Solitons.Text.RegularExpressions;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Solitons.Collections;

namespace Solitons.CommandLine.Models;

internal sealed record CliOptionModel
{
    public string Name { get; }
    private const string AliasPattern = @"(?:-{1,2}[\w\?]+)";
    private const string PipeDelimiter = "|";
    private const string CommaDelimiter = ",";
    private static readonly string KeyValuePairPattern;
    private static readonly string ValueOptionPattern;

    private static readonly Regex ValidAliasesPsvRegex = new(
        @$"^{AliasPattern}(?:\{PipeDelimiter}{AliasPattern})*$",
        RegexOptions.Compiled |
        RegexOptions.CultureInvariant);

    static CliOptionModel()
    {
        KeyValuePairPattern = @$"(?:$dot-notation)|(?:$accessor-notation)"
            .Replace("$dot-notation", @"\.(?:$key(?:\s+(?:$value)?)?)?")
            .Replace("$accessor-notation", @"\[(?:$key (?:\] (?: \s+ (?:$value)? )? )? )? ")
            .Replace("$key", @"\S+")
            .Replace("$value", @"[^\s\-]\S*")
            .Convert(RegexUtils.RemoveWhitespace)
            .Convert(RegexUtils.EnsureNonCapturingGroup);
        Debug.Assert(RegexUtils.IsValidExpression(KeyValuePairPattern));

        ValueOptionPattern = @$"(?:[^\s\-]\S*)?";
        Debug.Assert(RegexUtils.IsValidExpression(ValueOptionPattern));

    }

    public CliOptionModel(
        string pipeSeparatedAliases,
        string name,
        string description,
        bool isRequired)
    {
        Name = ThrowIf.ArgumentNullOrWhiteSpace(name).Trim();
        RegexGroupName = CliModel.GenerateRegexGroupName(Name);
        Description = ThrowIf.ArgumentNullOrWhiteSpace(description).Trim();
        IsRequired = isRequired;
        pipeSeparatedAliases = pipeSeparatedAliases
            .DefaultIfNullOrWhiteSpace(Name)
            .Convert(RegexUtils.RemoveWhitespace);

        if (false == ValidAliasesPsvRegex.IsMatch(pipeSeparatedAliases))
        {
            throw new ArgumentException(nameof(pipeSeparatedAliases),
                $"Invalid alias format: '{pipeSeparatedAliases}' must be pipe-separated words.");
        }

        var aliases = pipeSeparatedAliases
            .Split(PipeDelimiter, StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(alias => alias.Length)
            .ThenBy(alias => alias, StringComparer.OrdinalIgnoreCase)
            .ToImmutableArray();

        if (aliases.Length != pipeSeparatedAliases.Split(PipeDelimiter).Length)
        {
            throw new ArgumentException($"Duplicate aliases found: {string.Join(CommaDelimiter, aliases)}", nameof(pipeSeparatedAliases));
        }

        Aliases = [.. aliases];
        PipeDelimitedAliases = aliases.Join("|");
        Synopsis = aliases
            .OrderBy(a => a.Length)
            .ThenBy(a => a, StringComparer.OrdinalIgnoreCase)
            .Join(", ");


        RegexPattern = aliases
            .Select(a => a.Replace("?", @"\?"))
            .Join(PipeDelimiter)
            .Convert(pattern =>
            {
                var optionKey = RegexUtils.EnsureNonCapturingGroup(pattern);
                var optionValue = FluentArray.Create(
                    $@"(?<{RegexGroupName}>{KeyValuePairPattern})",
                    $@"(?:\s+(?<{RegexGroupName}>{ValueOptionPattern}))")
                    .Join(PipeDelimiter)
                    .Convert(RegexUtils.EnsureNonCapturingGroup);
                return @$"{optionKey}(?:{optionValue})";
            });
        Debug.Assert(RegexUtils.IsValidExpression(RegexPattern));
    }

    public ImmutableArray<string> Aliases { get; }

    public bool IsRequired { get; }

    public string PipeDelimitedAliases { get; }

    public string Synopsis { get; }

    public string RegexPattern { get; }

    public string RegexGroupName { get; }

    public string Description { get; }

    public string ToCsv(bool includeSpaceAfterComma = false)
    {
        return Aliases
            .OrderBy(a => a.Length)
            .ThenBy(a => a)
            .Join(includeSpaceAfterComma ? ", " : ",");
    }

    public override string ToString() => Synopsis;
}
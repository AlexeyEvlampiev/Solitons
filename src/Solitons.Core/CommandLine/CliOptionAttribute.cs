using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

/// <summary>
/// Attribute to define command line options for a method or property.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public class CliOptionAttribute : Attribute, ICliOptionMetadata
{
    private static readonly Regex SpecificationRegex;
    private static readonly Regex OptionRegex;

    static CliOptionAttribute()
    {
        SpecificationRegex = new Regex(
            @"^$option(\|$option)*$"
            .Replace("$option", "(?<option>--?[^|]+)"));
        OptionRegex = new Regex(@"(?<=^[-]{1,2})[\w\?]+(?:[-]+[\w\?]+)*$");
    }

    /// <summary>
    /// Constructs a CLI option attribute with specified options and an optional description.
    /// </summary>
    /// <param name="specification">A pipe-separated string containing different CLI options.</param>
    /// <param name="description">A description of what the options do.</param>
    /// <exception cref="ArgumentException">Thrown when the options format is invalid or contains duplicates.</exception>
    public CliOptionAttribute(string specification, string description = "")
    {
        // Regex to split the pattern and validate format
        var namePatternMatch = ThrowIf
            .ArgumentNullOrWhiteSpace(specification)
            .Convert(SpecificationRegex.Match);

        if (!namePatternMatch.Success)
        {
            throw new ArgumentException("Invalid pattern format. Expected: --option1|--option2|-o|...",
                nameof(specification));
        }

        var names = new HashSet<string>(StringComparer.Ordinal);
        var longNames = new List<string>();
        var shortNames = new List<string>();

        foreach (Capture capture in namePatternMatch.Groups["option"].Captures)
        {
            var match = OptionRegex.Match(capture.Value);
            var value = match.Value;
            if (false == match.Success)
            {
                throw new ArgumentException($"Invalid parameter name option: {capture}",
                    nameof(specification));
            }

            if (!names.Add(capture.Value))
            {
                throw new ArgumentException($"Duplicate option name detected: '{value}'", nameof(specification));
            }

            if (capture.Value.StartsWith("--"))
            {
                longNames.Add(value);
            }
            else
            {
                shortNames.Add(value);
            }

        }

        if (longNames.Count == 0 && shortNames.Count == 0)
        {
            throw new ArgumentException("At least one long or short name must be specified.", nameof(specification));
        }


        Description = description;
        Aliases = names.ToArray().AsReadOnly();
        LongOptionNames = longNames.AsReadOnly();
        ShortOptionNames = shortNames.AsReadOnly();
        Description = description;

        OptionPipeAliases = LongOptionNames
            .OrderByDescending(n => n.Length)
            .Select(n => $"--{n}")
            .Union(ShortOptionNames
                .OrderByDescending(n => n.Length)
                .Select(n => $"-{n}"))
            .Join("|");

        OptionNamesCsv = ShortOptionNames
            .Select(o => $"-{o}")
            .Union(LongOptionNames.Select(o => $"--{o}"))
            .Join(", ");
    }

    /// <summary>
    /// Comma-separated list of all options.
    /// </summary>
    public string OptionNamesCsv { get; }

    /// <summary>
    /// Specification of options as used in the CLI.
    /// </summary>
    public string OptionPipeAliases { get; }

    /// <summary>
    /// Description of the CLI options.
    /// </summary>
    public string Description { get; }

    public IReadOnlyList<string> Aliases { get; }

    /// <summary>
    /// List of short option names.
    /// </summary>
    public IReadOnlyList<string> ShortOptionNames { get; }

    /// <summary>
    /// List of long option names.
    /// </summary>
    public IReadOnlyList<string> LongOptionNames { get; }

    public virtual bool AllowsCsv => true;


    public virtual TypeConverter? GetCustomTypeConverter(out string inputSample)
    {
        inputSample = String.Empty;
        return null;
    }

    /// <summary>
    /// Returns a string representation of the option specification.
    /// </summary>
    /// <returns>A string that represents the option specification.</returns>
    public override string ToString() => OptionPipeAliases;

    public virtual StringComparer GetValueComparer() => StringComparer.OrdinalIgnoreCase;
}
using Solitons.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Solitons.Collections;

namespace Solitons.CommandLine.Models;

internal sealed record CliOptionModel
{
    public string Name { get; }
    private const string AliasPattern = @"(?:\-{1,2}[\w\?][\w\-\?]*)";
    private const string PipeDelimiter = "|";
    private const string CommaDelimiter = ",";
    private static readonly string KeyValuePairPattern;
    private static readonly string ValueOptionPattern;

    private static readonly Regex ValidAliasesPsvRegex = new(
        @$"^{AliasPattern}(?:\{PipeDelimiter}{AliasPattern})*$",
        RegexOptions.Compiled |
        RegexOptions.CultureInvariant);

    sealed record Settings
    {
        [DebuggerStepThrough]
        public Settings(ParameterInfo parameter)
        {
            var attributes = parameter.GetCustomAttributes().ToArray();
            var methodInfo = (MethodInfo)parameter.Member;

            Name = parameter.Name.DefaultIfNullOrWhiteSpace("none");
            IsRequired =
                attributes.OfType<RequiredAttribute>().Any() ||
                (false == parameter.IsOptional);
            Description = attributes
                .OfType<DescriptionAttribute>()
                .Select(a => a.Description)
                .Concat(attributes.OfType<CliOptionAttribute>().Select(a => a.Description))
                .Where(d => d.IsPrintable())
                .FirstOrDefault($"{parameter.Name} parameter of the {methodInfo.Name} command handler.");
            PipeSeparatedAliases = attributes.OfType<CliOptionAttribute>().Select(a => a.PipeSeparatedAliases)
                .Concat([Name])
                .First();
        }

        public Settings(PropertyInfo property, object[] attributes)
        {
            var bundleType = ThrowIf.NullReference(property.DeclaringType);
            Name = property.Name.DefaultIfNullOrWhiteSpace("none");
            IsRequired =
                attributes.OfType<RequiredAttribute>().Any();
            Description = attributes
                .OfType<DescriptionAttribute>()
                .Select(a => a.Description)
                .Concat(attributes.OfType<CliOptionAttribute>().Select(a => a.Description))
            .Where(d => d.IsPrintable())
                .FirstOrDefault($"{property.Name} property of the {bundleType.FullName} options bundle.");
            PipeSeparatedAliases = attributes.OfType<CliOptionAttribute>().Select(a => a.PipeSeparatedAliases)
                .Concat([Name])
                .First();
        }

        public string PipeSeparatedAliases { get; }
        public string Name { get; }
        public string Description { get; }
        public bool IsRequired { get; }
    };

    

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

    [DebuggerStepThrough]
    public CliOptionModel(ParameterInfo parameter) : this(new Settings(parameter))
    {
        Provider = parameter;
    }

    [DebuggerStepThrough]
    private CliOptionModel(Settings settings) : this(
        settings.PipeSeparatedAliases, 
        settings.Name, 
        settings.Description, 
        settings.IsRequired)
    {
    }

    public CliOptionModel(
        string pipeSeparatedAliases,
        string name,
        string description,
        bool required)
    {
        Name = ThrowIf.ArgumentNullOrWhiteSpace(name).Trim();
        RegexGroupName = CliModel.GenerateRegexGroupName(Name);
        Description = ThrowIf.ArgumentNullOrWhiteSpace(description).Trim();
        Required = required;
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


    public static IEnumerable<CliOptionModel> FromBundle(Type bundleType, CliCommandModel command)
    {
        ThrowIf.False(CliOptionBundle.IsAssignableFrom(bundleType));
        var options = bundleType
            .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Select(p => new
            {
                PropertyInfo = p,
                Attributes = p.GetCustomAttributes(true)
            })
            .Where(i => i.Attributes.OfType<CliOptionAttribute>().Any());
        foreach (var option in options)
        {
            var setting = new Settings(option.PropertyInfo, option.Attributes);
            yield return new CliOptionModel(setting)
            {
                Provider = option.PropertyInfo,
                Command = command
            };
        }
    }


    public ImmutableArray<string> Aliases { get; }

    public bool Required { get; }

    public string PipeDelimitedAliases { get; }

    public string Synopsis { get; }

    public string RegexPattern { get; }

    public string RegexGroupName { get; }

    public string Description { get; }

    public required CliCommandModel Command { get; init; }

    public string ToCsv(bool includeSpaceAfterComma = false)
    {
        return Aliases
            .OrderBy(a => a.Length)
            .ThenBy(a => a)
            .Join(includeSpaceAfterComma ? ", " : ",");
    }

    public required ICustomAttributeProvider Provider { get; init; }

    public override string ToString() => Synopsis;

}
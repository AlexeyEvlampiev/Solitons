using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

/// <summary>
/// Represents a schema for defining and parsing command-line actions.
/// </summary>
internal sealed class CliActionSchema
{
    private readonly List<object> _items = new();
    private readonly Regex _validRegexGroupNameRegex = new(@"(?is-m)^[a-z]\w*$");
    private readonly Regex _validSubCommandAliasRegex = new(@"^\w[\w\-]*$");
    private readonly Regex _validOptionAliasRegex = new(@"^--?\w[\w\-]*$");

    /// <summary>
    /// Matches the specified command line string against the schema.
    /// </summary>
    /// <param name="commandLine">The command line string to match.</param>
    /// <returns>A <see cref="Match"/> object that contains information about the match.</returns>
    public Match Match(string commandLine)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Calculates the rank of the specified command line based on the schema.
    /// </summary>
    /// <param name="commandLine">The command line string to rank.</param>
    /// <returns>An integer representing the rank.</returns>
    public int Rank(string commandLine)
    {
        string pattern = new CliActionRegexMatchRankerRtt(this);
#if DEBUG
        pattern = Regex.Replace(pattern, @"(?<=\S)[^\S\r\n]{2,}", " ");
        pattern = Regex.Replace(pattern, @"(?<=\n)\s*\n", "");
#endif
        var regex = new Regex(pattern,
            RegexOptions.Compiled |
            RegexOptions.IgnorePatternWhitespace |
            RegexOptions.Singleline);

        var match = regex.Match(commandLine);
        var groups = match.Groups
            .OfType<Group>()
            .Where(g => g.Success)
            .Skip(1) // Exclude group 0 from count
            .ToList();
        int rank = groups.Count;
        return rank;
    }

    public IEnumerable<ICommandSegment> CommandSegments => _items
        .OfType<ICommandSegment>();


    public IEnumerable<IOption> Options => _items
        .OfType<IOption>();

    public CliActionSchema AddSubCommand(IEnumerable<string> aliases)
    {
        aliases = aliases
            .Where(s => s.IsPrintable())
            .Select(s => s.Trim())
            .ToList();
        var invalidAliasesCsv = aliases
            .Where(a => false == _validSubCommandAliasRegex.IsMatch(a))
            .Select(a => $"'{a}'")
            .Join(",");
        if (invalidAliasesCsv.IsPrintable())
        {
            throw new InvalidOperationException(
                $"Invalid sub-command aliases detected: {invalidAliasesCsv}. " +
                "Each alias must start with a word character and can include hyphens.");
        }
        if (aliases.Any())
        {
            _items.Add(new SubCommand(aliases));
        }
        return this;
    }

    public CliActionSchema AddArgument(string regexGroupName)
    {
        AssertRegexGroupName(regexGroupName);
        _items.Add(new Argument(regexGroupName, _items.OfType<ICommandSegment>()));
        return this;
    }

    public CliActionSchema AddFlagOption(string regexGroupName, IReadOnlyList<string> aliases)
    {
        AssertRegexGroupName(regexGroupName);
        AssertOptionAliases(aliases);
        _items.Add(new Option(regexGroupName, aliases, OptionType.Flag));
        return this;
    }


    public CliActionSchema AddScalarOption(string regexGroupName, IReadOnlyList<string> aliases)
    {
        AssertRegexGroupName(regexGroupName);
        AssertOptionAliases(aliases);
        _items.Add(new Option(regexGroupName, aliases, OptionType.Scalar));
        return this;
    }

    public CliActionSchema AddVectorOption(string regexGroupName, IReadOnlyList<string> aliases)
    {
        AssertRegexGroupName(regexGroupName);
        AssertOptionAliases(aliases);
        _items.Add(new Option(regexGroupName, aliases, OptionType.Vector));
        return this;
    }

    public CliActionSchema AddMapOption(string regexGroupName, IReadOnlyList<string> aliases)
    {
        AssertRegexGroupName(regexGroupName);
        AssertOptionAliases(aliases);
        _items.Add(new Option(regexGroupName, aliases, OptionType.Map));
        return this;
    }

    private void AssertOptionAliases(IReadOnlyList<string> aliases)
    {
        var invalidAliasesCsv = aliases
            .Where(a => false == _validOptionAliasRegex.IsMatch(a))
            .Select(a => $"'{a}'")
            .Join(",");
        if (invalidAliasesCsv.IsPrintable())
        {
            throw new InvalidOperationException($"Invalid option aliases detected: {invalidAliasesCsv}");
        }
    }


    private void AssertRegexGroupName(string groupName)
    {
        if (false == _validRegexGroupNameRegex.IsMatch(groupName))
        {
            throw new InvalidOperationException($"The regex group name '{groupName}' is invalid.");
        }
    }
    public interface ICommandSegment
    {
        string BuildRegularExpression();

        public sealed bool IsArgument => this is Argument;
    }

    public interface IOption
    {
        string BuildRegularExpression();
    }

    public abstract class Token(IEnumerable<string> aliases)
    {
        public IReadOnlyList<string> Aliases { get; } = aliases.ToArray();
    }

    public sealed record Argument(string RegexGroupName, IEnumerable<ICommandSegment> SubCommands) : ICommandSegment
    {
        public string BuildRegularExpression()
        {
            var segments = SubCommands.ToList();
            var selfIndex = segments.IndexOf(this);
            if (selfIndex == -1)
            {
                throw new InvalidOperationException();
            }

            var preCondition = segments
                .Take(selfIndex)
                .Select(cs => cs.BuildRegularExpression())
                .Select(p => $"(?:{p})")
                .Join("\\s+")
                .Convert(p => p.IsPrintable() ? @$"(?<=(?:{p})\s+)" : string.Empty)
                .Convert(lookBehind
                    =>
                {
                    var lookAhead = segments
                        .OfType<SubCommand>()
                        .Select(sc => sc.BuildRegularExpression())
                        .Join("|")
                        .Convert(x => $"(?!(?:{x}))");
                    return $"{lookBehind}{lookAhead}";
                });
            

            var postCondition = segments
                .Skip(selfIndex + 1)
                .Select(cs => cs.BuildRegularExpression())
                .Select(p => $"(?:{p})")
                .Join("\\s+")
                .Convert(p => p.IsPrintable() ? @$"(?=\s+(?:{p}))" : string.Empty);

            var pattern = $@"{preCondition}(?<{RegexGroupName}>[^\s-]\S*){postCondition}";
            return pattern;
        }
    }



    public sealed class SubCommand(IEnumerable<string> aliases) : Token(aliases), ICommandSegment
    {
        public string BuildRegularExpression()
        {
            var valueExp = aliases
                .Select(a => a.Trim().ToLower())
                .Distinct(StringComparer.Ordinal)
                .OrderByDescending(a => a.Length)
                .Join("|")
                .DefaultIfNullOrWhiteSpace("$?");
            return valueExp;
        }
    }

    public enum OptionType
    {
        Flag,
        Scalar,
        Vector,
        Map
    }

    public sealed class Option(string regexGroupName, IEnumerable<string> aliases, OptionType optionType) : Token(aliases), IOption
    {
        public string RegexGroupName { get; } = regexGroupName;
        public OptionType OptionType { get; } = optionType;
        public string BuildRegularExpression()
        {
            var token = aliases
                .Select(a => a.Trim().ToLower())
                .Distinct(StringComparer.Ordinal)
                .OrderByDescending(a => a.Length)
                .Join("|");
            ThrowIf.NullOrWhiteSpace(token);
            switch (OptionType)
            {
                case (OptionType.Flag):
                    return $@"(?<{RegexGroupName}>{token})";
                case (OptionType.Scalar):
                    return $@"(?:{token})\s*(?<{RegexGroupName}>(?:[^\s-]\S*)?)";
                case (OptionType.Map):
                {
                    var pattern = $@"(?:{token})(?:$dot-notation|$accessor-notation)"
                        .Replace(@"$dot-notation", @$"\.(?<{RegexGroupName}>(?:\S+\s+[^\s-]\S+)?)")
                        .Replace(@"$accessor-notation", @$"(?<{RegexGroupName}>(?:\[\S+\]\s+[^\s-]\S+)?)");
                    return pattern;
                }
                default:
                    throw new NotSupportedException() ;
            }
        }
    }
}
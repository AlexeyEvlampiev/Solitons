using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal sealed class CliActionSchema
{
    private readonly List<object> _items = new();

    public Match Match(string commandLine)
    {
        throw new NotImplementedException();
    }

    public int Rank(string commandLine, int optimalMatchRank = 10)
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
        int rank = 0;
        foreach (var group in groups)
        {
            rank += group.Name.Equals(CliActionRegexMatchRankerRtt.OptimalMatchGroupName, StringComparison.Ordinal)
                ? optimalMatchRank
                : 1;
        }

        return rank;
    }

    public IEnumerable<ICommandSegment> CommandSegments => _items
        .OfType<ICommandSegment>();


    public IEnumerable<IOption> Options => _items
        .OfType<IOption>();

    public CliActionSchema AddSubCommand(IEnumerable<string> aliases)
    {
        _items.Add(new SubCommand(aliases));
        return this;
    }

    public CliActionSchema AddArgument(string regexGroupName)
    {
        _items.Add(new Argument(regexGroupName, _items.OfType<SubCommand>()));
        return this;
    }

    public CliActionSchema AddFlagOption(string regexGroupName, IEnumerable<string> aliases)
    {
        _items.Add(new Option(regexGroupName, aliases, OptionType.Flag));
        return this;
    }

    public CliActionSchema AddScalarOption(string regexGroupName, IEnumerable<string> aliases)
    {
        _items.Add(new Option(regexGroupName, aliases, OptionType.Scalar));
        return this;
    }

    public CliActionSchema AddVectorOption(string regexGroupName, IEnumerable<string> aliases)
    {
        _items.Add(new Option(regexGroupName, aliases, OptionType.Vector));
        return this;
    }

    public CliActionSchema AddMapOption(string regexGroupName, IEnumerable<string> aliases)
    {
        _items.Add(new Option(regexGroupName, aliases, OptionType.Map));
        return this;
    }


    public interface ICommandSegment
    {
        string BuildRegularExpression();
    }

    public interface IOption
    {
        string RegexGroupName { get; }
        OptionType OptionType { get; }
        string BuildRegularExpression();
    }

    public abstract class Token(IEnumerable<string> aliases)
    {
        public IReadOnlyList<string> Aliases { get; } = aliases.ToArray();
    }

    public sealed record Argument(string RegexGroupName, IEnumerable<SubCommand> SubCommands) : ICommandSegment
    {
        public string BuildRegularExpression()
        {
            var valueExp = SubCommands
                .SelectMany(sc => sc.Aliases)
                .Select(a => a.Trim().ToLower())
                .Distinct(StringComparer.Ordinal)
                .OrderByDescending(a => a.Length)
                .Join("|")
                .DefaultIfNullOrWhiteSpace("$")
                .Convert(exp => $@"(?!(?:{exp}))[^\s-]\S*");
            return $"(?<{RegexGroupName}>{valueExp})";
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
                    return $@"(?:{token}\s+)(?<{RegexGroupName}>(?:[^\s-]\S*)?)";
                case (OptionType.Map):
                {
                    var pattern = $@"(?:{token})(?:$don-notation|$accessor-notation)"
                        .Replace(@"$don-notation", @$"\.(?<{RegexGroupName}>(?:\S+\s+[^\s-]\S+)?)")
                        .Replace(@"$accessor-notation", @$"(?<{RegexGroupName}>(?:\[\S+\]\s+[^\s-]\S+)?)");
                    return pattern;
                }
                default:
                    throw new NotSupportedException() ;
            }
        }
    }
}
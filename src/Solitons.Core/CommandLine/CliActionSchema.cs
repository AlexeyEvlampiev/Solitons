using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

/// <summary>
/// Represents a schema for defining and parsing command-line actions.
/// </summary>
internal sealed class CliActionSchema
{
    private readonly List<object> _fields = new();
    private readonly Regex _regex;
    private readonly Regex _rankRegex;


    public CliActionSchema(Action<Builder> config)
    {
        var builder = new Builder(this);
        config.Invoke(builder);

        var pattern = new CliActionRegularExpressionRtt(this)
            .ToString()
            .Convert(Beautify);

        var rankPattern = new CliActionRegexMatchRankerRtt(this)
            .ToString()
            .Convert(Beautify);

        _regex = new Regex(pattern,
            RegexOptions.Compiled |
            RegexOptions.Singleline |
            RegexOptions.IgnorePatternWhitespace);

        
        _rankRegex = new Regex(rankPattern,
            RegexOptions.Compiled |
            RegexOptions.Singleline |
            RegexOptions.IgnorePatternWhitespace);

        CommandFullPath = ThrowIf.NullOrWhiteSpace(null);
        string Beautify(string exp)
        {
#if DEBUG
            exp = Regex.Replace(exp, @"(?<=\S)[^\S\r\n]{2,}", " ");
            exp = Regex.Replace(exp, @"(?<=\n)\s*\n", "");
#endif
            return exp;
        }
    }

    /// <summary>
    /// Matches the specified command line string against the schema.
    /// </summary>
    /// <param name="commandLine">The command line string to match.</param>
    /// <param name="preProcessor"></param>
    /// <param name="unrecognizedTokensHandler"></param>
    /// <returns>A <see cref="Match"/> object that contains information about the match.</returns>
    public Match Match(
        string commandLine, 
        ICliTokenSubstitutionPreprocessor preProcessor,
        Action<ISet<string>> unrecognizedTokensHandler)
    {
        var match = _regex.Match(commandLine);
        var unrecognizedParameterGroup = GetUnrecognizedTokens(match);
        if (unrecognizedParameterGroup.Success)
        {
            var unrecognizedTokens = unrecognizedParameterGroup
                .Captures
                .Select(c => c.Value.Trim())
                .Select(preProcessor.GetSubstitution)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            unrecognizedTokensHandler.Invoke(unrecognizedTokens);
        }
        return match;
    }

    [DebuggerNonUserCode]
    public bool IsMatch(string commandLine) => _regex.IsMatch(commandLine);

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

        var match = _rankRegex.Match(commandLine);
        var groups = match.Groups
            .OfType<Group>()
            .Where(g => g.Success)
            .Skip(1) // Exclude group 0 from count
            .ToList();
        int rank = groups.Count;
        return rank;
    }

    public IEnumerable<ICommandSegment> CommandSegments => _fields
        .OfType<ICommandSegment>();


    public IEnumerable<IOption> Options => _fields
        .OfType<IOption>();

    public string CommandFullPath { get; }


    public class Builder
    {
        private readonly Regex _validRegexGroupNameRegex = new(@"(?is-m)^[a-z]\w*$");
        private readonly Regex _validSubCommandAliasRegex = new(@"^\w[\w\-]*$");
        private readonly Regex _validOptionAliasRegex = new(@"^(?:--?\w[\w\-]*|-\?)$");
        private readonly CliActionSchema _schema;

        internal Builder(CliActionSchema schema)
        {
            _schema = schema;
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


        public Builder AddSubCommand(IEnumerable<string> aliases)
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
                _schema._fields.Add(new SubCommand(aliases));
            }
            return this;
        }



        public Builder AddArgument(string regexGroupName)
        {
            AssertRegexGroupName(regexGroupName);
            _schema._fields.Add(new Argument(regexGroupName, _schema._fields.OfType<ICommandSegment>()));
            return this;
        }

        public Builder AddFlagOption(string regexGroupName, IReadOnlyList<string> aliases)
        {
            AssertRegexGroupName(regexGroupName);
            AssertOptionAliases(aliases);
            _schema._fields.Add(new Option(regexGroupName, aliases, CliOperandArity.Flag));
            return this;
        }


        public Builder AddScalarOption(string regexGroupName, IReadOnlyList<string> aliases)
        {
            AssertRegexGroupName(regexGroupName);
            AssertOptionAliases(aliases);
            _schema._fields.Add(new Option(regexGroupName, aliases, CliOperandArity.Scalar));
            return this;
        }

        public Builder AddVectorOption(string regexGroupName, IReadOnlyList<string> aliases)
        {
            AssertRegexGroupName(regexGroupName);
            AssertOptionAliases(aliases);
            _schema._fields.Add(new Option(regexGroupName, aliases, CliOperandArity.Vector));
            return this;
        }

        public Builder AddMapOption(string regexGroupName, IReadOnlyList<string> aliases)
        {
            AssertRegexGroupName(regexGroupName);
            AssertOptionAliases(aliases);
            _schema._fields.Add(new Option(regexGroupName, aliases, CliOperandArity.Map));
            return this;
        }

        [DebuggerStepThrough]
        public Builder AddOption(string regexGroupName, CliOperandArity arity, IReadOnlyList<string> aliases)
        {
            if (arity == CliOperandArity.Flag)
            {
                AddFlagOption(regexGroupName, aliases);
            }
            else if (arity == CliOperandArity.Scalar)
            {
                AddScalarOption(regexGroupName, aliases);
            }
            else if (arity == CliOperandArity.Vector)
            {
                AddVectorOption(regexGroupName, aliases);
            }
            else if (arity == CliOperandArity.Map)
            {
                AddMapOption(regexGroupName, aliases);
            }
            else
            {
                throw new InvalidOperationException();
            }

            return this;
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
                .Convert(p => p.IsPrintable() ? @$"(?<={p}\s+)" : string.Empty)
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
                .Select(cs =>
                {
                    if (cs is SubCommand cmd)
                    {
                        return cmd.BuildRegularExpression();
                    }
                    return @"[^\\s-]\\S*";
                })
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


    public sealed class Option(string regexGroupName, IEnumerable<string> aliases, CliOperandArity arity) : Token(aliases), IOption
    {
        public string RegexGroupName { get; } = regexGroupName;
        public CliOperandArity Arity { get; } = arity;
        public string BuildRegularExpression()
        {
            var token = aliases
                .Select(a => a.Trim().ToLower())
                .Distinct(StringComparer.Ordinal)
                .OrderByDescending(a => a.Length)
                .Join("|");
            ThrowIf.NullOrWhiteSpace(token);
            switch (Arity)
            {
                case (CliOperandArity.Flag):
                    return $@"(?<{RegexGroupName}>{token})";
                case (CliOperandArity.Scalar):
                    return $@"(?:{token})\s*(?<{RegexGroupName}>(?:[^\s-]\S*)?)";
                case (CliOperandArity.Map):
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

    private Group GetUnrecognizedTokens(Match match) => match.Groups[CliActionRegularExpressionRtt.UnrecognizedToken];

}
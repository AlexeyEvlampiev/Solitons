using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Solitons.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal sealed class CliActionSchema : ICliActionSchema
{
    interface ISegment
    {
        string RegularExpression { get; }
    }

    sealed record Command : ISegment
    {
        [DebuggerStepThrough]
        private Command(string psvPattern)
        {
            Debug.Assert(psvPattern.IsPrintable());
            var aliases = psvPattern.Contains("|")
                ? psvPattern.Split('|')
                : [psvPattern];

            var duplicatesCsv = aliases
                .Select(a => a.ToLower())
                .GroupBy(a => a)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .Join(", ");

            if (duplicatesCsv.IsPrintable())
            {
                throw new CliConfigurationException("Oops");
            }

            Aliases = aliases
                .OrderBy(i => i.Length)
                .ThenBy(i => i)
                .ToArray();

            AliasesPsv = aliases
                .OrderBy(i => i.Length)
                .ThenBy(i => i)
                .Join("|");

            AliasesCsv = aliases
                .OrderBy(i => i.Length)
                .ThenBy(i => i)
                .Join(", ");

            RegularExpression = aliases
                .OrderByDescending(i => i.Length)
                .ThenBy(i => i)
                .Join("|")
                .Convert(p => $"(?:{p})");
        }



        public static IEnumerable<Command> ToEnumerable(string psvPattern)
        {
            return Regex
                .Split(ThrowIf.ArgumentNull(psvPattern), @"\s+")
                .Where(s => s.IsPrintable())
                .Select(s => new Command(s));
        }

        public string AliasesPsv { get; }

        public string AliasesCsv { get; }

        public IReadOnlyList<string> Aliases { get; }
        public string RegularExpression { get; }

        public override string ToString() => AliasesPsv;
    }

    sealed record Argument : ISegment
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IReadOnlyList<ISegment> _segments;

        public Argument(
            string name, 
            IReadOnlyList<ISegment> segments)
        {
            _segments = segments;
            Name = name;
            Index = segments.Count;
        }

        public string Name { get; }

        public int Index { get; }

        public required string Description { get; init; }
        public string RegularExpression => BuildRegularExpression();

        private string BuildRegularExpression()
        {
            Debug.Assert(Index == _segments.IndexOf(this));
            var argValuePattern = @"[^\s\-]\S*";
            var lookAhead = _segments
                .OfType<Command>()
                .Select(c => RegularExpression)
                .Concat([argValuePattern])
                .Select(RegexUtils.EnsureNonCapturingGroup)
                .Join("|")
                .Convert(p => $"(?!{p})");
            var lookBehind = _segments
                .Take(Index)
                .Select(s =>
                {
                    if (s is Command cmd)
                    {
                        return cmd.RegularExpression;
                    }

                    return argValuePattern;
                })
                .Select(RegexUtils.EnsureNonCapturingGroup)
                .Join(@"\s+");

            return @$"{lookAhead}{lookBehind}{argValuePattern}";
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    private readonly List<ISegment> _segments = new(10);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly HashSet<string> _reservedCommandAliases = new(StringComparer.OrdinalIgnoreCase);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly HashSet<string> _registeredArgumentNames = new(StringComparer.OrdinalIgnoreCase);

    public CliActionSchema() {}


    public int SegmentsCount => _segments.Count;
    public int ArgumentsCount { get; }
    public int ExamplesCount { get; }
    public string Description { get; }

    public string GetSegmentRegularExpression(int segmentIndex)
    {
        if (segmentIndex < 0 || segmentIndex >= _segments.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(segmentIndex));
        }

        var segment = _segments[segmentIndex];
        return segment.RegularExpression;
    }

    public string GetSegmentRegularExpression()
    {
        return _segments
            .Select(s => s.RegularExpression)
            .Select(RegexUtils.EnsureNonCapturingGroup)
            .Join(@"\s+")
            .Convert(RegexUtils.EnsureNonCapturingGroup);
    }


    public string GetSynopsis()
    {
        return _segments
            .Select(s =>
            {
                if (s is Command cmd)
                {
                    return cmd.AliasesPsv;
                }

                if (s is Argument arg)
                {
                    return $"<{arg.Name}>";
                }

                throw new InvalidOperationException();
            })
            .Join(" ")
            .Trim();
    }

    public bool IsArgument(int segmentIndex) => _segments[segmentIndex] is Argument;
    public IEnumerable<ICliActionSchema.Argument> Arguments { get; }
    public IEnumerable<ICliActionSchema.Option> Options { get; }
    public IEnumerable<ICliActionSchema.Example> Examples { get; }

    public ICliActionSchema.Argument GetArgument(int argumentIndex)
    {
        throw new NotImplementedException();
    }

    public ICliActionSchema.Example GetExample(int exampleIndex)
    {
        throw new NotImplementedException();
    }

    public void AddRoutePsvExpression(string psvPattern)
    {
        Command
            .ToEnumerable(psvPattern)
            .ForEach(Add);
    }

    public void AddArgument(string name, string description)
    {
        name = ThrowIf
            .ArgumentNullOrWhiteSpace(name)
            .Trim();
        description = ThrowIf
            .ArgumentNullOrWhiteSpace(description)
            .Trim();

        if (false == _registeredArgumentNames.Add(name))
        {
            throw new CliConfigurationException("Oops");
        }

        var argument = new Argument(name, _segments)
        {
            Description = description
        };

        _segments.Add(argument);
    }

    private void Add(Command cmd)
    {
        foreach (var alias in cmd.Aliases)
        {
            if (false == _reservedCommandAliases.Add(alias))
            {
                throw new CliConfigurationException($"Oops...");
            }
        }

        _segments.Add(cmd);
    }
}
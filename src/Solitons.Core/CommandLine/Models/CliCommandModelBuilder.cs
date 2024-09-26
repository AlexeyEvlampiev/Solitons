using Solitons.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine.Models;

internal class CliCommandModelBuilder
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

            var lookBehind = _segments
                .Take(Index)
                .Select(s =>
                {
                    if (s is Command cmd)
                    {
                        return cmd.RegularExpression;
                    }

                    return @"[^\s\-]\S*";
                })
                .Select(RegexUtils.EnsureNonCapturingGroup)
                .Join(@"\s+")
                .Convert(RegexUtils.EnsureNonCapturingGroup)
                .Convert(p => @$"(?<={p}\s+)");

            var lookAhead = _segments
                .OfType<Command>()
                .Select(c => c.RegularExpression)
                .Concat([@"[\-]"])
                .Select(RegexUtils.EnsureNonCapturingGroup)
                .Join("|")
                .Convert(p => $"(?!{p})");


            return @$"{lookBehind}{lookAhead}\S+";
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
    private readonly List<ISegment> _commandSegments = new(10);

    [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
    private readonly List<object> _options = new(10);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly HashSet<string> _reservedCommandAliases = new(StringComparer.OrdinalIgnoreCase);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly HashSet<string> _registeredArgumentNames = new(StringComparer.OrdinalIgnoreCase);

    public CliCommandModelBuilder()
    {
        
    }

    public required string Description { get; init; }
        

    public CliCommandModel Build()
    {
        return new CliCommandModel()
        {
            Description = Description,
            Synopsis = "",
            CommandSegments = ImmutableArray<CliCommandSegmentModel>.Empty
        };
    }
}
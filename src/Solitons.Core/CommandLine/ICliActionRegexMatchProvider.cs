using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Solitons.Collections;

namespace Solitons.CommandLine;

internal interface ICliActionRegexMatchProvider
{
    IEnumerable<CliCommandSegmentData> GetCommandSegments();



    [DebuggerStepThrough]
    public sealed int Rank(string commandLine, int optimalMatchRank = 100) => CliActionRegexMatchRankerRtt
        .Rank(commandLine, optimalMatchRank, this);
}

internal abstract record CliCommandSegmentData();

internal abstract record CliOptionData();

internal sealed record CliSubCommandData : CliCommandSegmentData
{
    public CliSubCommandData(string alias)
    {
        Aliases = [..FluentArray.Create(alias)];
    }

    public CliSubCommandData(params string[] aliases)
    {
        Aliases = [..aliases];
    }

    public string ToRegularExpression() => Aliases
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(a => a.ToLower().Trim())
            .OrderByDescending(a => a.Length)
            .Join("|")
            .DefaultIfNullOrWhiteSpace("(?:$)*");

    public ImmutableArray<string> Aliases { get; init; }

    public void Deconstruct(out ImmutableArray<string> Aliases)
    {
        Aliases = this.Aliases;
    }
}

internal sealed record CliArgumentData() : CliCommandSegmentData
{
    public string ToRegularExpression(IEnumerable<CliSubCommandData> subCommands) => subCommands
        .Select(sc => sc.ToRegularExpression())
        .Join("|")
        .Convert(p => $@"(?!(?:{p})\b)[^-]\S+");
}

internal sealed record CliFlagOptionData : CliOptionData;
internal sealed record CliScalarOptionData : CliOptionData;

internal sealed record CliVectorOptionData : CliOptionData;

internal sealed record CliMapOptionData : CliOptionData;
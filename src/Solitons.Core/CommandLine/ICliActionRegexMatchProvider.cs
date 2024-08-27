using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Solitons.Collections;

namespace Solitons.CommandLine;

internal interface ICliActionRegexMatchProvider
{
    IEnumerable<CliActionRegexMatchCommandSegment> GetCommandSegments();

    IEnumerable<CliActionRegexMatchOption> GetOptions();

    [DebuggerStepThrough]
    public sealed int Rank(string commandLine, int optimalMatchRank = 100) => CliActionRegexMatchRankerRtt
        .Rank(commandLine, optimalMatchRank, this);
}

internal abstract record CliActionRegexMatchCommandSegment();

internal abstract record CliActionRegexMatchOption
{
    [DebuggerNonUserCode]
    protected CliActionRegexMatchOption(string groupName, string[] aliases)
    {
        GroupName = groupName;
        Aliases = [..aliases];
    }

    public string GroupName { get; init; }
    public ImmutableArray<string> Aliases { get; }

    public abstract string ToRegularExpression();
}

internal sealed record CliActionRegexMatchCommandToken : CliActionRegexMatchCommandSegment
{
    public CliActionRegexMatchCommandToken(string alias)
    {
        Aliases = [..FluentArray.Create(alias)];
    }

    public CliActionRegexMatchCommandToken(params string[] aliases)
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

    public void Deconstruct(out ImmutableArray<string> aliases)
    {
        aliases = this.Aliases;
    }
}

internal sealed record CliActionRegexMatchCommandArgument() : CliActionRegexMatchCommandSegment
{
    public string ToRegularExpression(IEnumerable<CliActionRegexMatchCommandToken> subCommands) => subCommands
        .Select(sc => sc.ToRegularExpression())
        .Join("|")
        .Convert(p => $@"(?!(?:{p})\b)[^-]\S+");
}

internal sealed record CliActionRegexMatchFlagOption : CliActionRegexMatchOption
{
    [DebuggerNonUserCode]
    public CliActionRegexMatchFlagOption(string groupName, string[] aliases) 
        : base(groupName, aliases)
    {
    }

    public override string ToRegularExpression()
    {
        return Aliases
            .OrderByDescending(a => a.Length)
            .Join("|")
            .Convert(p => $@"(?<{GroupName}>{p})(?=\s|$)");
    }
}

internal sealed record CliActionRegexMatchScalarOption : CliActionRegexMatchOption
{
    public CliActionRegexMatchScalarOption(string groupName, string[] aliases) : base(groupName, aliases)
    {
    }

    public override string ToRegularExpression()
    {
        throw new NotImplementedException();
    }

}

internal sealed record CliActionRegexMatchVectorOption : CliActionRegexMatchOption
{
    public CliActionRegexMatchVectorOption(string groupName, string[] aliases) : base(groupName, aliases)
    {
    }

    public override string ToRegularExpression()
    {
        throw new NotImplementedException();
    }

    public void Deconstruct(out string GroupName)
    {
        GroupName = this.GroupName;
    }
}

internal sealed record CliActionRegexMatchMapOption : CliActionRegexMatchOption
{
    public CliActionRegexMatchMapOption(string groupName, string[] aliases) : base(groupName, aliases)
    {
    }

    public override string ToRegularExpression()
    {
        throw new NotImplementedException();
    }

    public void Deconstruct(out string GroupName)
    {
        GroupName = this.GroupName;
    }
}
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;


internal interface ICliActionRegexMatchProvider
{
    IEnumerable<CommandSegment> GetCommandSegments();

    IEnumerable<Option> GetOptions();

    int Rank(string commandLine, int optimalMatchRank = 100);

    internal static string JoinAliases(string[] aliases) =>
        aliases
            .Where(a => a.IsPrintable())
            .Select(a => a.Trim().ToLower())
            .Distinct(StringComparer.Ordinal)
            .Select(a => Regex.Replace(a, @"[?]", "[?]"))
            .OrderByDescending(a => a.Length)
            .Join("|")
            .DefaultIfNullOrWhiteSpace("(?:$)*");

    public static SubCommand CreateSubCommand(IEnumerable<string> aliases) => new([..aliases]);

    internal abstract record CommandSegment();
    internal sealed record SubCommand(ImmutableArray<string> Aliases) : CommandSegment;
    internal sealed record Argument() : CommandSegment;

    internal abstract record Option(ImmutableArray<string> Aliases);
    internal sealed record FlagOption(ImmutableArray<string> Aliases) : Option(Aliases);
    internal sealed record ScalarOption(ImmutableArray<string> Aliases) : Option(Aliases);
    internal sealed record VectorOption(ImmutableArray<string> Aliases, bool EnableCsv) : Option(Aliases);
    internal sealed record MapOption(ImmutableArray<string> Aliases) : Option(Aliases);
}


internal abstract record CliActionRegexMatchCommandSegment();



internal abstract record CliActionRegexMatchOption
{
    [DebuggerNonUserCode]
    protected CliActionRegexMatchOption(string groupName, string[] aliases)
    {
        RegularExpressionGroupName = groupName;
        Aliases = [..aliases];
        AliasesRegularExpression = ICliActionRegexMatchProvider.JoinAliases(aliases);
    }

    public string RegularExpressionGroupName { get; }
    public ImmutableArray<string> Aliases { get; }
    public string AliasesRegularExpression { get; }

    public abstract string ToRegularExpression();
}

internal sealed record CliActionRegexMatchCommandToken : CliActionRegexMatchCommandSegment
{
    [DebuggerStepThrough]
    public CliActionRegexMatchCommandToken(string alias) : this([alias]) { }

    public CliActionRegexMatchCommandToken(params string[] aliases)
    {
        Aliases = [..aliases];
        AliasesRegularExpression = ICliActionRegexMatchProvider.JoinAliases(aliases);
    }

    public string ToRegularExpression() => AliasesRegularExpression;

    public ImmutableArray<string> Aliases { get;  }


    public string AliasesRegularExpression { get; }
}

internal sealed record CliActionRegexMatchCommandArgument() : CliActionRegexMatchCommandSegment
{
    public string ToRegularExpression(IEnumerable<CliActionRegexMatchCommandToken> subCommands) => subCommands
        .Select(sc => sc.ToRegularExpression())
        .Join("|")
        .Convert(p => $@"(?!(?:{p})\b)[^-]\S*");
}

internal sealed record CliActionRegexMatchFlagOption : CliActionRegexMatchOption
{
    [DebuggerNonUserCode]
    public CliActionRegexMatchFlagOption(string groupName, string[] aliases) 
        : base(groupName, aliases)
    {
    }

    public override string ToRegularExpression() => 
        $@"(?<{RegularExpressionGroupName}>{AliasesRegularExpression})(?=\s|$)";
}

internal sealed record CliActionRegexMatchScalarOption : CliActionRegexMatchOption
{
    public CliActionRegexMatchScalarOption(string groupName, string[] aliases) : base(groupName, aliases) { }

    public override string ToRegularExpression() =>
        $@"(?:{AliasesRegularExpression})(?:\s+(?<{RegularExpressionGroupName}>[^-]\S*))?";
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
}

internal sealed record CliActionRegexMatchMapOption : CliActionRegexMatchOption
{
    public CliActionRegexMatchMapOption(string groupName, string[] aliases) : base(groupName, aliases)
    {
    }

    public override string ToRegularExpression()
    {
        return @$"(?:{AliasesRegularExpression})(?:$dot-notation|$accessor-notation)?"
            .Replace("$dot-notation", @$"\.(?<{RegularExpressionGroupName}>\S+(?:\s+[^-]\S*)?)")
            .Replace("$accessor-notation", @$"(?<{RegularExpressionGroupName}>\[\S*\](?:\s+[^-]\S*)?)");
    }
}
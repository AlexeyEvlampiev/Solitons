using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Solitons.Text.RegularExpressions;

namespace Solitons.CommandLine.Models;

internal sealed record CliArgumentModel
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly IReadOnlyList<object> _segments;

    public CliArgumentModel(
        string name,
        string description,
        ParameterInfo parameterInfo,
        IReadOnlyList<object> segments)
    {
        _segments = segments;
        Name = name;
        Description = description;
        ParameterInfo = parameterInfo;
        Display = $"<{Name.ToUpperInvariant()}>";
        RegexGroupName = parameterInfo
            .Name
            .DefaultIfNullOrWhiteSpace(name)
            .Convert(CliModel.GenerateRegexGroupName);

    }

    public string Name { get; }
    public string Description { get; }
    public ParameterInfo ParameterInfo { get; }

    public string RegexGroupName { get; }

    public string Display { get; }

    public string RegexPattern
    {
        [DebuggerStepThroughAttribute]
        get => BuildRegexPattern();
    }

    private string BuildRegexPattern()
    {
        var index = _segments.IndexOf(this);
        ThrowIf.False(_segments.Contains(this));
        ThrowIf.False(index >= 0);
        var notArgumentPattern = _segments
            .OfType<CliRouteSubcommandModel>()
            .Select(subcommand => subcommand.RegexPattern)
            .Concat([@"\-"])
            .Select(RegexUtils.EnsureNonCapturingGroup)
            .Join("|")
            .Convert(RegexUtils.EnsureNonCapturingGroup);
        

        var lookBehind = _segments
            .Take(index)
            .Select(segment =>
            {
                if (segment is CliRouteSubcommandModel subcommand)
                {
                    return subcommand.RegexPattern;
                }

                return $@"(?!{notArgumentPattern})\S+";
            })
            .Join(@"\s+")
            .Convert(RegexUtils.EnsureNonCapturingGroup)
            .Convert(p => @$"(?<={p}\s+)");

        var lookAhead = notArgumentPattern
            .Convert(RegexUtils.EnsureNonCapturingGroup)
            .Convert(p => $"(?!{p})");
        return @$"(?<{RegexGroupName}>{lookBehind}{lookAhead}\S+)";
    }

    public override string ToString() => Display;
}
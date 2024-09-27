using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Solitons.Text.RegularExpressions;

namespace Solitons.CommandLine.Models;

internal sealed record CliArgumentModel : ICliCommandSegmentModel
{
    private const string PipeDelimiter = "|";

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly IReadOnlyList<object> _segments;

    public CliArgumentModel(
        string name,
        string description,
        ParameterInfo parameterInfo,
        IReadOnlyList<object> segments)
    {
        _segments = ThrowIf.ArgumentNull(segments);
        Name = ThrowIf.ArgumentNullOrWhiteSpace(name).Trim();
        Description = ThrowIf.ArgumentNullOrWhiteSpace(description).Trim();
        ParameterInfo = parameterInfo;
        Synopsis = $"<{Name.ToUpperInvariant()}>";
        RegexGroupName = parameterInfo
            .Name
            .DefaultIfNullOrWhiteSpace(name)
            .Convert(CliModel.GenerateRegexGroupName);
    }

    public string Name { get; }
    public string Description { get; }
    public ParameterInfo ParameterInfo { get; }

    public string RegexGroupName { get; }

    public string Synopsis { get; }

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
            .Concat([@$"\{PipeDelimiter}"])
            .Select(RegexUtils.EnsureNonCapturingGroup)
            .Join(PipeDelimiter)
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
        var pattern = @$"(?<{RegexGroupName}>{lookBehind}{lookAhead}\S+)";
        Debug.Assert(RegexUtils.IsValidExpression(pattern));
        return pattern;
    }

    public override string ToString() => Synopsis;
    string ICliCommandSegmentModel.ToSynopsis() => Synopsis;
}
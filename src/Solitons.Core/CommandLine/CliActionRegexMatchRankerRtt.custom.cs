using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal partial class CliActionRegexMatchRankerRtt
{
    internal static readonly string ExecutableGroupName;
    internal static readonly string OptimalMatchGroupName;

    static CliActionRegexMatchRankerRtt()
    {
        var postfix = typeof(CliActionRegexMatchRankerRtt).GUID.ToString("N");
        ExecutableGroupName = $"executable_{postfix}";
        OptimalMatchGroupName = $"optimal_{postfix}";
    }


    internal CliActionRegexMatchRankerRtt(CliActionSchema schema)
    {
        var segments = schema
            .CommandSegments
            .ToArray();

        CommandSegmentRegularExpressions = segments
            .Select(segment => segment.BuildRegularExpression())
            .ToArray();

        OptionRegularExpressions = schema
            .Options
            .Select(option => option.BuildRegularExpression())
            .ToArray();
    }


    public string Pattern { get; }
    private IReadOnlyList<string> CommandSegmentRegularExpressions { get; }
    public IReadOnlyList<string> OptionRegularExpressions { get; }

    public static int Rank(string commandLine, int optimalMatchRank, CliActionSchema schema)
    {
        string pattern = new CliActionRegexMatchRankerRtt(schema);
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
            rank += group.Name.Equals(OptimalMatchGroupName, StringComparison.Ordinal)
                ? optimalMatchRank
                : 1;
        }

        return rank;
    }


}
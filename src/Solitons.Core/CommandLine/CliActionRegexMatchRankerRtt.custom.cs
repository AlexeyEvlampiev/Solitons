using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal partial class CliActionRegexMatchRankerRtt
{
    private static readonly string ExecutableGroupName;
    private static readonly string OptimalMatchGroupName;

    static CliActionRegexMatchRankerRtt()
    {
        var postfix = typeof(CliActionRegexMatchRankerRtt).GUID.ToString("N");
        ExecutableGroupName = $"executable_{postfix}";
        OptimalMatchGroupName = $"optimal_{postfix}";
    }


    private CliActionRegexMatchRankerRtt(ICliActionRegexMatchProvider provider)
    {
        var segments = provider
            .GetCommandSegments()
            .ToArray();

        CommandSegmentRegularExpressions = segments
            .Select(segment =>
            {
                if (segment is CliActionRegexMatchCommandToken cmd)
                {
                    return cmd.ToRegularExpression();
                }

                if (segment is CliActionRegexMatchCommandArgument arg)
                {
                    return arg.ToRegularExpression(segments.OfType<CliActionRegexMatchCommandToken>());
                }

                throw new InvalidOperationException("Oops...");
            })
            .ToArray();

        OptionRegularExpressions = provider
            .GetOptions()
            .Select(option => option.ToRegularExpression())
            .ToArray();
    }


    public string Pattern { get; }
    private IReadOnlyList<string> CommandSegmentRegularExpressions { get; }
    public IReadOnlyList<string> OptionRegularExpressions { get; }

    public static int Rank(string commandLine, int optimalMatchRank, ICliActionRegexMatchProvider provider)
    {
        string pattern = new CliActionRegexMatchRankerRtt(provider);
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
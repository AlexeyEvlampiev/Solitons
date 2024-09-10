using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Joins;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal partial class CliActionRegexMatchRankerRtt
{
    private CliActionRegexMatchRankerRtt(
        IReadOnlyList<ICliRouteSegment> routeSegments,
        IReadOnlyList<JazzyOptionInfo> options)
    {
        CommandSegmentRegularExpressions = routeSegments
            .Select((segment, index) => segment.IsArgument 
                ? segment.BuildRegularExpression() 
                : $"(?<{GenGroupName(index)}>{segment.BuildRegularExpression()})")
            .ToArray();

        OptionRegularExpressions = options
            .Select(option => option.RegularExpression)
            .ToArray();
    }

    private string GenGroupName(int index) => $"segment_{GetType().GUID:N}_{index}";

    private IReadOnlyList<string> CommandSegmentRegularExpressions { get; }
    public IReadOnlyList<string> OptionRegularExpressions { get; }
    public string OptimalMatchGroupName => $"optimal_match_group_{GetType().GUID:N}";

    [DebuggerStepThrough]
    public static string ToString(
        IReadOnlyList<ICliRouteSegment> routeSegments,
        IReadOnlyList<JazzyOptionInfo> options)
    {
        string expression = new CliActionRegexMatchRankerRtt(routeSegments, options);
#if DEBUG
        expression = Regex.Replace(expression, @"(?<=\S)[^\S\r\n]{2,}", " ");
        expression = Regex.Replace(expression, @"(?<=\n)\s*\n", "");
#endif
        Debug.WriteLine(expression);
        return expression;
    }
}
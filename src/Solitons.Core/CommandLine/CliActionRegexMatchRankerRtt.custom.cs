using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal partial class CliActionRegexMatchRankerRtt
{
    private CliActionRegexMatchRankerRtt(
        ICliActionSchema schema)
    {
        CommandSegmentRegularExpressions = Enumerable
            .Range(0, schema.CommandSegmentsCount)
            .Select(segmentIndex =>
            {
                var expression = schema.GetSegmentRegularExpression(segmentIndex);
                if (schema.IsArgumentSegment(segmentIndex))
                {
                    return $"(?<{GenGroupName(segmentIndex)}>{expression})";
                }

                return expression;
            })
            .ToArray();

        OptionRegularExpressions = Enumerable
            .Range(0, schema.OptionsCount)
            .Select(schema.GetOptionRegularExpression)
            .ToArray();
    }

    private string GenGroupName(int index) => $"segment_{GetType().GUID:N}_{index}";

    private IReadOnlyList<string> CommandSegmentRegularExpressions { get; }
    public IReadOnlyList<string> OptionRegularExpressions { get; }
    public static string OptimalMatchGroupName => $"optimal_match_group_{typeof(CliActionRegexMatchRankerRtt).GUID:N}";

    [DebuggerStepThrough]
    public static string ToString(
        ICliActionSchema schema)
    {
        string expression = new CliActionRegexMatchRankerRtt(schema);
#if DEBUG
        expression = Regex.Replace(expression, @"(?<=\S)[^\S\r\n]{2,}", " ");
        expression = Regex.Replace(expression, @"(?<=\n)\s*\n", "");
#endif
        Debug.WriteLine(expression);
        return expression;
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Solitons.CommandLine;

internal partial class CliActionRegularExpressionRtt
{
    public static readonly string UnrecognizedToken = $"unrecognized_token_{typeof(CliActionRegularExpressionRtt).GUID:N}";

    private CliActionRegularExpressionRtt(
        ICliActionSchema patternBuilder,
        IReadOnlyList<CliOptionInfo> options)
    {
        CommandSegmentRegularExpressions = Enumerable
            .Range(0, patternBuilder.SegmentsCount)
            .Select(segmentIndex =>
            {
                var expression = patternBuilder.GetSegmentRegularExpression(segmentIndex);
                if (patternBuilder.IsArgument(segmentIndex))
                {
                    return expression;
                }
                return $"(?<{GenGroupName(segmentIndex)}>{expression})";
            })
            .ToArray();

        OptionRegularExpressions = options
            .Select(option => option.RegularExpression)
            .ToArray();
    }

    private string GenGroupName(int index) => $"segment_{GetType().GUID:N}_{index}";

    private IReadOnlyList<string> CommandSegmentRegularExpressions { get; }
    public IReadOnlyList<string> OptionRegularExpressions { get; }

    [DebuggerStepThrough]
    public static string ToString(
        ICliActionSchema patternBuilder)
    {
        throw new NotImplementedException();
        //string expression = new CliActionRegularExpressionRtt(
        //    patternBuilder, 
        //    options);
        //Debug.WriteLine(expression);
        //return expression;
    }
}
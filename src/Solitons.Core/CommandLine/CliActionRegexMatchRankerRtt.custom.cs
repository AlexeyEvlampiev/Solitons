using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal partial class CliActionRegexMatchRankerRtt
{
    private CliActionRegexMatchRankerRtt(
        ICliActionSchema schema,
        IReadOnlyList<CliOptionInfo> options)
    {
        throw new NotImplementedException();
        //CommandSegmentRegularExpressions = routeSegments
        //    .Select((segment, index) => segment.IsArgument 
        //        ? segment.BuildRegularExpression(routeSegments) 
        //        : $"(?<{GenGroupName(index)}>{segment.BuildRegularExpression(routeSegments)})")
        //    .ToArray();

        //OptionRegularExpressions = options
        //    .Select(option => option.RegularExpression)
        //    .ToArray();
    }

    private string GenGroupName(int index) => $"segment_{GetType().GUID:N}_{index}";

    private IReadOnlyList<string> CommandSegmentRegularExpressions { get; }
    public IReadOnlyList<string> OptionRegularExpressions { get; }
    public static string OptimalMatchGroupName => $"optimal_match_group_{typeof(CliActionRegexMatchRankerRtt).GUID:N}";

    [DebuggerStepThrough]
    public static string ToString(
        ICliActionSchema schema)
    {
        throw new NotImplementedException();
//        string expression = new CliActionRegexMatchRankerRtt(routeSegments, options);
//#if DEBUG
//        expression = Regex.Replace(expression, @"(?<=\S)[^\S\r\n]{2,}", " ");
//        expression = Regex.Replace(expression, @"(?<=\n)\s*\n", "");
//#endif
//        Debug.WriteLine(expression);
//        return expression;
    }
}
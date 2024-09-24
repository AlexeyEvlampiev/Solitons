using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Solitons.CommandLine;

internal partial class CliActionRegularExpressionRtt
{
    public static readonly string UnrecognizedToken = $"unrecognized_token_{typeof(CliActionRegularExpressionRtt).GUID:N}";

    private CliActionRegularExpressionRtt(
        IReadOnlyList<ICliRouteSegmentMetadata> routeSegments,
        IReadOnlyList<CliOptionInfo> options)
    {
        CommandSegmentRegularExpressions = routeSegments
            .Select((segment, index) =>
            {
                var expression = segment.BuildRegularExpression(routeSegments);
                if (segment.IsArgument)
                {
                    return expression;
                }
                return $"(?<{GenGroupName(index)}>{expression})";
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
        IReadOnlyList<ICliRouteSegmentMetadata> routeSegments,
        IReadOnlyList<CliOptionInfo> options)
    {
        string expression = new CliActionRegularExpressionRtt(
            routeSegments, 
            options);
        Debug.WriteLine(expression);
        return expression;
    }
}
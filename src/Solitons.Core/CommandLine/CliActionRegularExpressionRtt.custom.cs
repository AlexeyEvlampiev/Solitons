using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Solitons.CommandLine;

internal partial class CliActionRegularExpressionRtt
{
    public static readonly string UnrecognizedToken = $"unrecognized_token_{typeof(CliActionRegularExpressionRtt).GUID:N}";

    private CliActionRegularExpressionRtt(
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

    [DebuggerStepThrough]
    public static string ToString(
        ICliActionSchema schema)
    {
        string expression = new CliActionRegularExpressionRtt(
            schema);
        Debug.WriteLine(expression);
        return expression;
    }
}
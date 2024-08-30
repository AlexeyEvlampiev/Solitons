using System.Collections.Generic;
using System.Linq;

namespace Solitons.CommandLine;

internal partial class CliActionRegularExpressionRtt
{
    public static readonly string UnrecognizedToken = $"unrecognized_token_{typeof(CliActionRegularExpressionRtt).GUID:N}";

    internal CliActionRegularExpressionRtt(CliActionSchema schema)
    {
        var segments = schema
            .CommandSegments
            .ToArray();

        CommandSegmentRegularExpressions = segments
            .Select((segment, index) =>
            {
                var expression = segment.BuildRegularExpression();
                if (segment.IsArgument)
                {
                    return expression;
                }
                return  $"(?<{GenGroupName(index)}>{expression})";
            })
            .ToArray();

        OptionRegularExpressions = schema
            .Options
            .Select(option => option.BuildRegularExpression())
            .ToArray();
    }

    private string GenGroupName(int index) => $"segment_{GetType().GUID:N}_{index}";

    private IReadOnlyList<string> CommandSegmentRegularExpressions { get; }
    public IReadOnlyList<string> OptionRegularExpressions { get; }
}
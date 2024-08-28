using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal partial class CliActionRegexMatchRankerRtt
{
    internal CliActionRegexMatchRankerRtt(CliActionSchema schema)
    {
        var segments = schema
            .CommandSegments
            .ToArray();

        CommandSegmentRegularExpressions = segments
            .Select((segment, index) => segment.IsArgument 
                ? segment.BuildRegularExpression() 
                : $"(?<{GenGroupName(index)}>{segment.BuildRegularExpression()})")
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
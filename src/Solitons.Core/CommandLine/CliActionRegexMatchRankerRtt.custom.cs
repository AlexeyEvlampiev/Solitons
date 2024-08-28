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
            .Select(segment => segment.IsArgument 
                ? segment.BuildRegularExpression() 
                : $"({segment.BuildRegularExpression()})")
            .ToArray();

        OptionRegularExpressions = schema
            .Options
            .Select(option => option.BuildRegularExpression())
            .ToArray();
    }

    private IReadOnlyList<string> CommandSegmentRegularExpressions { get; }
    public IReadOnlyList<string> OptionRegularExpressions { get; }

}
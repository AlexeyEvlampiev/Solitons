using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine.Models;


internal sealed record CliCommandModel
{
    public CliCommandModel(
        MethodInfo methodInfo, 
        object? program,
        IReadOnlyList<CliOptionModel> masterOptions)
    {
        MethodInfo = methodInfo;
        Program = program;

        var registeredSegments = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var segments = new List<object>();

        var methodAtt = methodInfo.GetCustomAttributes().ToArray();
        foreach (var attribute in methodAtt)
        {
            if (attribute is CliRouteAttribute route)
            {
                var tag = new CliRouteSubcommandModel(route.PsvExpression);
                segments.Add(tag);
                var ambiguousTagsCsv = tag
                    .Aliases
                    .Where(registeredSegments.Contains)
                    .Join(", ");
                throw CliConfigurationException.AmbiguousCommandSegment(methodInfo, ambiguousTagsCsv);
            }
        }
    }

    public MethodInfo MethodInfo { get; }
    public object? Program { get; }

    //public required string Synopsis { get; init; }

    public string Description { get; }

    //public required ImmutableArray<CliCommandSegmentModel> CommandSegments { get; init; }
}

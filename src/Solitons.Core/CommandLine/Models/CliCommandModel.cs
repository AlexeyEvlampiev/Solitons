using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

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

        var parameters = methodInfo.GetParameters();
        var subcommandAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var segments = new List<object>();

        var methodAtt = methodInfo.GetCustomAttributes().ToArray();
        foreach (var attribute in methodAtt)
        {
            if (attribute is CliRouteAttribute route)
            {
                segments.AddRange(CliRouteSubcommandModel
                    .FromRoute(route.RouteDeclaration)
                    .Do(subcommand =>
                    {
                        var duplicatesCsv = subcommand
                            .Aliases
                            .Where(alias => false == subcommandAliases.Add(alias))
                            .Join(",");
                        if (duplicatesCsv.IsPrintable())
                        {
                            throw new CliConfigurationException("Oops...");
                        }
                    }));
            }
            else if (attribute is CliArgumentAttribute argument)
            {
                var parameter = parameters.Single(argument.References);
                var model = new CliArgumentModel(argument.Name, argument.Description, parameter, segments);
                segments.Add(model);
            }
        }

        CommandSegments = [.. segments];
    }

    public MethodInfo MethodInfo { get; }
    public object? Program { get; }

    //public required string Synopsis { get; init; }

    public string Description { get; }

    public ImmutableArray<object> CommandSegments { get; }
}

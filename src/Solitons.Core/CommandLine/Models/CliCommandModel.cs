using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Solitons.CommandLine.Models;


internal sealed record CliCommandModel
{
    public CliCommandModel(
        MethodInfo methodInfo, 
        object? program,
        MasterOptionFactory masterOptionFactory,
        IReadOnlyList<CliRouteSubcommandModel> baseSubcommands)
    {
        var parameters = methodInfo.GetParameters();
        var subcommandAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var segments = new List<ICliSynopsisModel>(){};
        segments.AddRange(baseSubcommands);

        var methodAtt = methodInfo.GetCustomAttributes().ToArray();
        var description = methodAtt
            .OfType<DescriptionAttribute>()
            .Select(a => a.Description)
            .Concat([methodInfo.Name])
            .First(d => d.IsPrintable());

        MethodInfo = methodInfo;
        Program = program;
        Description = description;

        
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
        Synopsis = segments
            .Select(s => s.ToSynopsis())
            .Join(" ");
        Examples = [.. methodAtt
            .OfType<CliCommandExampleAttribute>()
            .Select(a => new CliExampleModel()
            {
                Example = ThrowIf.NullOrWhiteSpace(a.Example).Trim(),
                Description = ThrowIf.NullOrWhiteSpace(a.Description).Trim()
            })
            .OrderBy(e => e.Example.Length)];

        var options = new List<CliOptionModel>();
        foreach (var parameter in parameters)
        {
            var attributes = parameter.GetCustomAttributes().ToArray();
            var optionAtt = attributes.OfType<CliOptionAttribute>().FirstOrDefault();
            if (CliOptionBundle.IsAssignableFrom(parameter.ParameterType))
            {
                if (optionAtt is not null)
                {
                    throw new CliConfigurationException("Oops");
                }

                options.AddRange(CliOptionModel.FromBundle(parameter.ParameterType, this));
            }
            else if (optionAtt is not null)
            {
                options.Add(new CliOptionModel(parameter)
                {
                    Command = this,
                    Provider = parameter
                });
            }
        }

        options.AddRange(masterOptionFactory(this));
        Options = [.. options];
    }

    public MethodInfo MethodInfo { get; }
    public object? Program { get; }

    public string Description { get; }

    public string Synopsis { get; }

    public ImmutableArray<ICliSynopsisModel> CommandSegments { get; }

    public ImmutableArray<CliOptionModel> Options { get; }

    public ImmutableArray<CliExampleModel> Examples { get; }

    public override string ToString() => Synopsis;
}

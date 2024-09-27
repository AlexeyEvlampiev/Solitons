using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace Solitons.CommandLine.Models;

internal sealed record CliModel
{
    public CliModel(
        IReadOnlyList<CliModule> sources,
        IReadOnlyList<CliMasterOptionBundle> masterOptionBundles,

    string description,
        string logo)
    {
        Logo = ThrowIf.ArgumentNull(logo).Trim();
        Description = ThrowIf.ArgumentNullOrWhiteSpace(description).Trim();

        var masterOptions = masterOptionBundles
            .SelectMany(CliOptionModel.GetOptions)
            .ToArray();

        var commands = new List<CliCommandModel>(10);
        foreach (var source in sources)
        {
            var sites = source
                .ProgramType
                .GetInterfaces()
                .Concat([source.ProgramType])
                .Distinct()
                .SelectMany(type => type.GetMethods(source.Binding))
                .Where(mi => mi
                    .GetCustomAttributes()
                    .OfType<CliRouteAttribute>()
                    .Any())
                .Select(mi => new CliCommandModel(mi, source.Program, masterOptions));
        }
    }

    public string Logo { get; }
    public string Description { get; }

    public ImmutableArray<CliCommandModel> Commands { get; }
}
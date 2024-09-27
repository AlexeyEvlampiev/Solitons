using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Solitons.CommandLine.Models;

internal sealed record CliModel
{
    private static long _sequenceNumber = 0;

    public CliModel(
        IReadOnlyList<CliModule> sources,
        IReadOnlyList<CliMasterOptionBundle> masterOptionBundles,

    string description,
        string logo,
        string baseRoute)
    {
        Logo = ThrowIf.ArgumentNull(logo).Trim();
        Description = ThrowIf.ArgumentNullOrWhiteSpace(description).Trim();
        var baseSubcommands = CliRouteSubcommandModel.FromRoute(baseRoute);

        var masterOptionsFactory = CliOptionModel.CreateMasterOptionFactory(masterOptionBundles);
        

        var commands = new List<CliCommandModel>(10);
        foreach (var source in sources)
        {
            var commandRange = source
                .ProgramType
                .GetInterfaces()
                .Concat([source.ProgramType])
                .Distinct()
                .SelectMany(type => type.GetMethods(source.Binding))
                .Where(mi => mi
                    .GetCustomAttributes()
                    .OfType<CliRouteAttribute>()
                    .Any())
                .Select(mi => new CliCommandModel(mi, source.Program, masterOptionsFactory, baseSubcommands));
            commands.AddRange(commandRange);
        }

        Commands = [..commands];
    }

    public string Logo { get; }
    public string Description { get; }

    public ImmutableArray<CliCommandModel> Commands { get; }


    public static string GenerateRegexGroupName(string operandName)
    {
        var suffix = Interlocked
            .Increment(ref _sequenceNumber)
            .ToString()
            .PadLeft(16, '0');
        return $"{operandName}_{suffix}";
    }
}
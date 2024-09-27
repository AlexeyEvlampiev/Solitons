using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using Solitons.Diagnostics;

namespace Solitons.CommandLine.Models;

internal sealed record CliModel
{
    private static long SequenceNumber = 0;
    public CliModel(
        IReadOnlyList<CliModule> sources,
        IReadOnlyList<CliMasterOptionBundle> masterOptionBundles,

    string description,
        string logo)
    {
        Logo = ThrowIf.ArgumentNull(logo).Trim();
        Description = ThrowIf.ArgumentNullOrWhiteSpace(description).Trim();

        //var masterOptions = masterOptionBundles
        //    .SelectMany(CliOptionModel.GetOptions)
        //    .ToArray();

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
                .Select(mi => new CliCommandModel(mi, source.Program, []));
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
            .Increment(ref SequenceNumber)
            .ToString()
            .PadLeft(16, '0');
        return $"{operandName}_{suffix}";
    }
}
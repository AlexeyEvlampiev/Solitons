using System.Collections.Generic;
using System.Linq;

namespace Solitons.CommandLine;

internal partial class CliGeneralHelpRtt
{
    internal sealed record Command(string Synopsis, string Description);
    private CliGeneralHelpRtt(
        ICliActionSchema[] schemas)
    {
        Commands = schemas
            .Select(s => new Command(s.GetSynopsis().DefaultIfNullOrWhiteSpace("''"), s.Description))
            .Distinct()
            .ToArray();
        SynopsisWidth = Commands.Max(cmd => cmd.Synopsis.Length) + 2;
    }

    internal IReadOnlyList<Command> Commands { get; }

    internal int SynopsisWidth { get; }

    public required string Logo { get; init; }
    public required string ProgramName { get; init; }
    public required string Description { get; init; }

    


    public static string Build(
        string logo, 
        string programName, 
        string description,
        ICliActionSchema[] schemas)
    {
        var list = new CliGeneralHelpRtt(schemas)
            {
                Logo = logo,
                ProgramName = programName,
                Description = description,
            }
            .ToString();
        return list;
    }
}
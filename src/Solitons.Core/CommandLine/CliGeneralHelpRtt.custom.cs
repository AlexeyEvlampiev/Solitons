using System;
using System.Collections.Generic;
using System.Linq;

namespace Solitons.CommandLine;

internal partial class CliGeneralHelpRtt
{

    private CliGeneralHelpRtt(IReadOnlyList<ICliAction> actions)
    {
        Commands = actions
            .Select(a =>
            {
                var segments = new List<string>();
                foreach (var segment in a.GetRouteSegments())
                {
                    if (segment is CliSubCommandInfo cmd &&
                        cmd.SubCommandPattern.IsPrintable())
                    {
                        segments.Add(cmd.SubCommandPattern);
                        
                    }
                    else if(segment is CliArgumentInfo argument)
                    {
                        segments.Add($"<{argument.ArgumentRole}>");
                    }
                }

                return segments.Join(" ");
            })
            .Order(StringComparer.OrdinalIgnoreCase)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyList<string> Commands { get; set; }

    public required string Logo { get; init; }
    public required string ProgramName { get; init; }
    public required string Description { get; init; }

    


    public static string Build(
        string logo, 
        string programName, 
        string description, 
        IReadOnlyList<ICliAction> actions)
    {
        var list = new CliGeneralHelpRtt(actions)
            {
                Logo = logo,
                ProgramName = programName,
                Description = description,
            }
            .ToString();
        return list;
    }
}
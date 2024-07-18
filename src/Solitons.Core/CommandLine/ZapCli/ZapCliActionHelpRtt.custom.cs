using System.Collections.Generic;
using System.Linq;

namespace Solitons.CommandLine.ZapCli;

internal partial class ZapCliActionHelpRtt
{
    private readonly CliAction _action;
    private const string Tab = "\t\t\t";

    private ZapCliActionHelpRtt(string tool, CliAction action)
    {
        _action = action;
        Tool = tool;
        Segments = action.CommandSegments;
        UsageOptions = CommandOptions(action.CommandSegments).ToList();

        Arguments = action
            .Operands
            .OfType<CliArgument>()
            .Select(o =>
            {
                return o
                    .Metadata
                    .OfType<CliArgumentAttribute>()
                    .Select(argument => $"<{argument.ArgumentRole.ToUpper()}>{Tab}{o.Description}")
                    .Single();
            })
            .ToList();


        Options = action
            .Operands
            .Where(o => o is not CliArgument)
            .Select(o =>
            {
                var option = o.CustomAttributes
                    .OfType<CliOptionAttribute>()
                    .FirstOrDefault(new CliOptionAttribute($"--{o.Name}", o.Description))!;
                return $"{option.OptionNamesCsv}{Tab}{o.Description}";
            })
            .ToList();


    }
    
    public IReadOnlyList<string> UsageOptions { get; }
    
    public IReadOnlyList<string> Options { get; }

    IEnumerable<string> CommandOptions(IEnumerable<ICliCommandSegment> segments)
    {
        var list = segments.ToList();
        var segment = list.FirstOrDefault();
        if (segment == null)
        {
            yield return "[options]";
            yield break;
        }
        
        var rhsOptions = CommandOptions(list.Skip(1)).ToList();
        if (segment is CliSubCommand command)
        {
            foreach (var option in command.Aliases)
            {
                foreach (var rhs in rhsOptions)
                {
                    yield return $"{option} {rhs}";
                }
            }
        }

        if (segment is CliArgument argument)
        {
            foreach (var rhs in rhsOptions)
            {
                yield return $"<{argument.ArgumentRole.ToUpper()}> {rhs}";
            }
        }
    }

    public string Tool { get; }
    public IEnumerable<object> Segments { get; }

    public IEnumerable<string> Arguments { get; }


    public static string Build(string tool, CliAction action)
    {
        var rtt = new ZapCliActionHelpRtt(tool, action);
        return rtt.ToString().Trim();
    }

}
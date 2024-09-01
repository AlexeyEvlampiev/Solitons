using System;
using System.Collections.Generic;
using System.Linq;

namespace Solitons.CommandLine;

internal partial class CliActionHelpRtt
{
    private readonly CliAction _action;
    private const string Tab = "   ";

    sealed record Example(int Index, string Description, string Command);

    private CliActionHelpRtt(string executableName, CliAction action)
    {
        _action = action;
        ExecutableName = executableName;
        Description = action.Description;

        throw new NotImplementedException();
        //Segments = action.CommandSegments;
        //UsageOptions = CommandOptions(action.CommandSegments).ToList();

        //Arguments = action
        //    .Operands
        //    .OfType<CliArgumentInfo>()
        //    .Select(o =>
        //    {
        //        return o
        //            .Metadata
        //            .OfType<CliArgumentAttribute>()
        //            .Select(argument => $"<{argument.ArgumentRole.ToUpper()}>{Tab}{o.Description}")
        //            .Single();
        //    })
        //    .ToList();


        //Options = action
        //    .Operands
        //    .Where(o => o is not CliArgumentInfo)
        //    .Select(o =>
        //    {
        //        var option = o.CustomAttributes
        //            .OfType<CliOptionAttribute>()
        //            .FirstOrDefault(new CliOptionAttribute($"--{o.Name}", o.Description))!;
        //        return $"{option.OptionNamesCsv}{Tab}{o.Description}";
        //    })
        //    .ToList();


    }

    public IReadOnlyList<string> UsageOptions { get; }

    public IReadOnlyList<string> Options { get; }

    IEnumerable<string> CommandOptions(IEnumerable<object> segments)
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

        if (segment is CliArgumentInfo argument)
        {
            foreach (var rhs in rhsOptions)
            {
                yield return $"<{argument.ArgumentRole.ToUpper()}> {rhs}";
            }
        }
    }

    public string ExecutableName { get; }
    public IEnumerable<object> Segments { get; }

    public IEnumerable<string> Arguments { get; }
    public string Description { get; }

    private IEnumerable<Example> Examples => _action.Examples
        .Select((item, index) => new Example(index + 1, item.Description, item.Example));


    public static string Build(string executableName, CliAction action)
    {
        var rtt = new CliActionHelpRtt(executableName, action);
        return rtt.ToString().Trim();
    }

    public static string Build(string executableName, IEnumerable<CliAction> actions)
    {
        return actions
            .Select(a => Build(executableName, a))
            .Join(Enumerable
                .Range(0, 3)
                .Select(_ => Environment.NewLine)
                .Join(""));
    }

}
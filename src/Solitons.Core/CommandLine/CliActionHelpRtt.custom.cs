using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


namespace Solitons.CommandLine;

internal partial class CliActionHelpRtt
{
    private readonly IReadOnlyList<IJazzExampleMetadata> _examples;
    private readonly CliAction _action;
    private const string Tab = "   ";

    sealed record Example(int Index, string Description, string Command);

    private CliActionHelpRtt(
        string description,
        IReadOnlyList<ICliRouteSegment> routeSegments,
        IReadOnlyList<JazzyOptionInfo> options,
        IReadOnlyList<IJazzExampleMetadata> examples)
    {
        _examples = examples;
        Description = description;


        Segments = routeSegments;
        UsageOptions = CommandOptions(routeSegments.OfType<CliSubCommandInfo>()).ToList();

        Arguments = routeSegments
            .OfType<CliArgumentInfo>()
            .Select(o =>
            {
                return o
                    .Metadata
                    .Convert(argument => $"<{argument.ArgumentRole.ToUpper()}>{Tab}{o.Description}");
            })
            .ToList();


        Options = options
            .Select(o => $"{o.CsvDeclaration}{Tab}{o.Description}")
            .ToList();
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
        if (segment is CliSubCommandInfo command)
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

    public IEnumerable<object> Segments { get; }

    public IEnumerable<string> Arguments { get; }
    public string Description { get; }

    private IEnumerable<Example> Examples => _examples
        .Select((item, index) => new Example(index + 1, item.Description, item.Example));


    [DebuggerStepThrough]
    public static string ToString(
        string description,
        IReadOnlyList<ICliRouteSegment> routeSegments,
        IReadOnlyList<JazzyOptionInfo> options,
        IReadOnlyList<IJazzExampleMetadata> examples)
    {
        string help = new CliActionHelpRtt(
            description,
            routeSegments, 
            options,
            examples);
        Debug.WriteLine(help);
        return help;
    }

    public static string ToString(IEnumerable<CliAction> actions)
    {
        return actions
            .Select(a => a.GetHelpText())
            .Join(Enumerable
                .Range(0, 3)
                .Select(_ => Environment.NewLine)
                .Join(""));
    }

}
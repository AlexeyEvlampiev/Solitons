using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


namespace Solitons.CommandLine;

internal partial class CliActionHelpRtt
{
    private readonly ICliActionSchema _schema;
    private const string Tab = "   ";

    sealed record Example(int Index, string Description, string Command);

    private CliActionHelpRtt(
        ICliActionSchema schema)
    {
        _schema = schema;

        throw new NotImplementedException();
        //Segments = routeSegments;
        //UsageOptions = CommandOptions(routeSegments.OfType<CliSubCommandInfo>()).ToList();

        //Arguments = routeSegments
        //    .OfType<CliArgumentInfo>()
        //    .Select(o =>
        //    {
        //        return o
        //            .SegmentMetadata
        //            .Convert(argument => $"<{argument.ArgumentRole.ToUpper()}>{Tab}{o.Description}");
        //    })
        //    .ToList();


        //Options = options
        //    .Select(o => $"{o.AliasCsvExpression}{Tab}{o.Description}")
        //    .ToList();
    }

    public IReadOnlyList<string> UsageOptions { get; }

    public IReadOnlyList<string> Options { get; }



    public IEnumerable<object> Segments { get; }

    public IEnumerable<string> Arguments { get; }
    public string Description => _schema.Description;

    private IEnumerable<Example> Examples => _schema
        .Examples
        .Select((item, index) => new Example(index + 1, item.Description, item.Command));


    [DebuggerStepThrough]
    public static string ToString(
        ICliActionSchema schema)
    {
        string help = new CliActionHelpRtt(schema);
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Solitons.CommandLine.Reflection;

namespace Solitons.CommandLine;

public sealed class CliTracingGlobalOptionBundle : CliGlobalOptionBundle
{
    private readonly List<TraceListener> _listenersSnapshot = new();

    [CliOption("--trace-level|--trace|-tl", "Trace level")]
    public TraceLevel Level { get; set; } = TraceLevel.Off;

    public override void OnExecutingAction(CliCommandLine commandLine)
    {
        _listenersSnapshot.Clear();
        _listenersSnapshot.AddRange(Trace.Listeners.OfType<TraceListener>());
        Trace.Listeners.Add(new CliTraceListener(Level));
        base.OnExecutingAction(commandLine);
    }

    public override void OnActionExecuted(CliCommandLine commandLine)
    {
        Trace.Listeners.Clear();
        Trace.Listeners.AddRange(_listenersSnapshot.ToArray());
        base.OnActionExecuted(commandLine);
    }
}
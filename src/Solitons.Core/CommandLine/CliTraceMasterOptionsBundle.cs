﻿using System.Diagnostics;
using System.Linq;
using Solitons.CommandLine.Reflection;

namespace Solitons.CommandLine;

internal sealed class CliTraceMasterOptionsBundle : CliMasterOptionBundle
{
    [CliOption("--trace-level|-trace", "Specifies the level of detail for tracing output (e.g., Off, Error, Warning, Info, Verbose).")]
    public TraceLevel TraceLevel { get; set; } = TraceLevel.Off;

    public override void OnExecutingAction(string commandLine)
    {
        base.OnExecutingAction(commandLine);
        var defaultListener = Trace.Listeners.OfType<DefaultTraceListener>().FirstOrDefault();
        Trace.Listeners.Clear();
        if (defaultListener != null)
        {
            Trace.Listeners.Add(defaultListener);
        }

        if (TraceLevel != TraceLevel.Off)
        {
            Trace.Listeners.Add(new CliTraceListener(TraceLevel));
        }
    }
}
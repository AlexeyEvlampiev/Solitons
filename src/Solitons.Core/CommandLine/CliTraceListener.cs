using System;
using System.Diagnostics;

namespace Solitons.CommandLine;

internal class CliTraceListener : TraceListener
{
    private readonly TraceLevel _level;

    public CliTraceListener(TraceLevel level)
    {
        _level = level;
    }

    public override void Write(string message)
    {
        // Assuming the least severe level for direct writes: Verbose
        if (ShouldTrace(TraceEventType.Verbose))
        {
            Console.Write(message);
        }
    }

    public override void WriteLine(string message)
    {
        // Assuming the least severe level for direct writes: Verbose
        if (ShouldTrace(TraceEventType.Verbose))
        {
            Console.WriteLine(message);
        }
    }

    public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
    {
        if (ShouldTrace(eventType))
        {
            Console.WriteLine($"{eventType}: {message}");
        }
    }

    public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
    {
        if (ShouldTrace(eventType))
        {
            if (args != null)
                Console.WriteLine($"{eventType}: {string.Format(format, args)}");
            else
                Console.WriteLine($"{eventType}: {format}");
        }
    }

    private bool ShouldTrace(TraceEventType eventType)
    {
        switch (_level)
        {
            case TraceLevel.Off:
                return false;
            case TraceLevel.Error:
                return eventType == TraceEventType.Error || eventType == TraceEventType.Critical;
            case TraceLevel.Warning:
                return eventType == TraceEventType.Warning || eventType == TraceEventType.Error || eventType == TraceEventType.Critical;
            case TraceLevel.Info:
                return eventType != TraceEventType.Verbose; // Info and more severe
            case TraceLevel.Verbose:
                return true; // All events
            default:
                return false;
        }
    }
}
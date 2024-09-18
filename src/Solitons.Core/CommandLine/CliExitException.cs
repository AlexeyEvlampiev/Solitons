using System;
using System.Diagnostics;

namespace Solitons.CommandLine;

[method: DebuggerNonUserCode]
internal sealed class CliExitException(int exitCode, string message) : Exception(message)
{
    public int ExitCode { get; } = exitCode;
}
using System;

namespace Solitons.CommandLine;

public sealed class CliExitException : Exception
{
    public CliExitException(string message, int exitCode = 1) : base(message)
    {
        ExitCode = exitCode;
    }

    public int ExitCode { get; }
}
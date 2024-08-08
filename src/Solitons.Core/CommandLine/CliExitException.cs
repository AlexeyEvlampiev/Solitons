using System;

namespace Solitons.CommandLine;

public sealed class CliExitException : Exception
{
    public CliExitException(string message) : base(message)
    {

    }

    public int ExitCode { get; init; } = 1;
}
using System;

namespace Solitons.CommandLine;

internal sealed class CliExitException(string message) : Exception(message)
{
    public int ExitCode { get; init; } = 1;
}
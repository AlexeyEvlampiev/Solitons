using System;

namespace Solitons.CommandLine;

public sealed class CliConfigurationException : Exception
{
    public CliConfigurationException(string message) : base(message)
    {
            
    }
}
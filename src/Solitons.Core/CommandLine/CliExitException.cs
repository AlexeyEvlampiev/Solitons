using System;

namespace Solitons.CommandLine;

internal sealed class CliExitException(string message) : Exception(message)
{
    public int ExitCode { get; init; } = 1;

    internal static CliExitException DictionaryOptionValueParseFailure(string option, string key, Type valueType)
    {
        return new CliExitException(
            $"Invalid input for option '{option}', key '{key}': the specified value could not be parsed to '{valueType.FullName}'.");
    }

    public static CliExitException InvalidDictionaryOptionKeyValueInput(string option, string capture)
    {
        return new CliExitException(
            $"Invalid input for option '{option}'. " +
            $"Expected a key-value pair but received '{capture}'. Please provide both a key and a value.");
    }
}
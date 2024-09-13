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

    public static CliExitException ConflictingOptionValues(string option)
    {
        return new CliExitException(
            $"The option '{option}' was specified with conflicting values. Specify a single value for this option and try again.");
    }

    public static CliExitException InvalidOptionInputParsing(string option, Type valueType)
    {
        return new CliExitException(
            $"The input for the option '{option}' is invalid. " +
            $"The value could not be parsed to the expected type '{valueType.FullName}'. " +
            "Please provide a valid input and try again.");
    }
}
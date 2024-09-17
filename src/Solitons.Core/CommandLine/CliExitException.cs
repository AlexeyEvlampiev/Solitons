using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

public class CliExitException : Exception
{
    protected internal CliExitException(string message) : base(message)
    {
    }


    public int ExitCode { get; init; } = 1;




    /// <summary>
    /// Returns an observable that throws a <see cref="CliExitException"/> with the specified exit code and error message when subscribed to.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence.</typeparam>
    /// <param name="exitCode">The exit code to return to the operating system.</param>
    /// <param name="message">The error message to include in the exception.</param>
    /// <returns>An observable that throws a <see cref="CliExitException"/>.</returns>
    [DebuggerNonUserCode]
    public static IObservable<T> Observable<T>(int exitCode, string message)
    {
        return System.Reactive.Linq.Observable.Throw<T>(new CliExitException(message)
        {
            ExitCode = exitCode
        });
    }

    /// <summary>
    /// Returns an observable that throws a <see cref="CliExitException"/> with a default exit code of 1 and the specified error message when subscribed to.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence.</typeparam>
    /// <param name="message">The error message to include in the exception.</param>
    /// <returns>An observable that throws a <see cref="CliExitException"/>.</returns>
    [DebuggerNonUserCode]
    public static IObservable<T> Observable<T>(string message)
    {
        return System.Reactive.Linq.Observable.Throw<T>(new CliExitException(message)
        {
            ExitCode = 1
        });
    }



    internal static CliExitException DictionaryOptionValueParseFailure(string option, string key, Type valueType)
    {
        return new CliExitException(
            $"Invalid input for option '{option}', key '{key}': the specified value could not be parsed to '{valueType.FullName}'.");
    }

    internal static CliExitException InvalidDictionaryOptionKeyValueInput(string option, string capture)
    {
        return new CliExitException(
            $"Invalid input for option '{option}'. " +
            $"Expected a key-value pair but received '{capture}'. Please provide both a key and a value.");
    }

    internal static CliExitException ConflictingOptionValues(string option)
    {
        return new CliExitException(
            $"The option '{option}' was specified with conflicting values. Specify a single value for this option and try again.");
    }

    internal static CliExitException InvalidOptionInputParsing(string option, Type valueType)
    {
        return new CliExitException(
            $"The input for the option '{option}' is invalid. " +
            $"The value could not be parsed to the expected type '{valueType.FullName}'. " +
            "Please provide a valid input and try again.");
    }

    internal static CliExitException CollectionOptionParsingFailure(string option, Type itemType)
    {
        return new CliExitException(
            $"Invalid input for the option '{option}': the provided value could not be parsed to the expected collection item type '{itemType.FullName}'. " +
            "Ensure the input is in the correct format and try again.");
    }

    internal static CliExitException DictionaryKeyMissingValue(string option, Group keyGroup)
    {
        return new CliExitException(
            $"The dictionary key '{keyGroup.Value}' for the option '{option}' is missing a corresponding value. " +
            "Please provide a value for the key and try again.");
    }

    internal static CliExitException DictionaryValueMissingKey(string option, Group valueGroup)
    {
        return new CliExitException(
            $"A key is missing for the value '{valueGroup.Value}' in the dictionary option '{option}'. " +
            "Please provide a corresponding key for this value and try again.");
    }
}
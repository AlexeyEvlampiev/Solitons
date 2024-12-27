using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

[method: DebuggerNonUserCode]
public class CliExitException(string message) : Exception(message)
{
    public int ExitCode { get; init; } = 1;

    public IObservable<T> AsObservable<T>() => Observable.Throw<T>(this);


    internal static CliExitException DictionaryOptionValueParseFailure(string option, string key, Type valueType) =>
        new($"Invalid input for option '{option}', key '{key}': the specified value could not be parsed to '{valueType.FullName}'.");

    internal static CliExitException InvalidDictionaryOptionKeyValueInput(string option, string capture) =>
        new(
            $"Invalid input for option '{option}'. " +
            $"Expected a key-value pair but received '{capture}'. Please provide both a key and a value.");

    internal static CliExitException ConflictingOptionValues(string option) =>
        new(
            $"The option '{option}' was specified with conflicting values. Specify a single value for this option and try again.");

    internal static CliExitException InvalidOptionInputParsing(string option, Type valueType) =>
        new(
            $"The input for the option '{option}' is invalid. " +
            $"The value could not be parsed to the expected type '{valueType.FullName}'. " +
            "Please provide a valid input and try again.");

    internal static CliExitException CollectionOptionParsingFailure(string option, Type itemType) =>
        new(
            $"Invalid input for the option '{option}': the provided value could not be parsed to the expected collection item type '{itemType.FullName}'. " +
            "Ensure the input is in the correct format and try again.");

    internal static CliExitException DictionaryKeyMissingValue(string option, Group keyGroup) =>
        new(
            $"The dictionary key '{keyGroup.Value}' for the option '{option}' is missing a corresponding value. " +
            "Please provide a value for the key and try again.");

    internal static CliExitException DictionaryValueMissingKey(string option, Group valueGroup) =>
        new(
            $"A key is missing for the value '{valueGroup.Value}' in the dictionary option '{option}'. " +
            "Please provide a corresponding key for this value and try again.");

  

    public static Func<Exception, IObservable<T>> AsObservable<T>(string message)
    {
        return Factory;
        IObservable<T> Factory(Exception exception)
        {
            return Observable.Throw<T>(new CliExitException(message));
        }
    }
}
using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading;

namespace Solitons.CommandLine;

/// <summary>
/// Provides functionality to manage exit signals within asynchronous call chains, allowing for 
/// graceful handling of exit codes and error messages in an observable manner.
/// </summary>
public static partial class CliExit
{
    [method: DebuggerNonUserCode]
    sealed class ExitException(int exitCode, string message) : Exception(message)
    {
        public int ExitCode { get; } = exitCode;
    }


    // AsyncLocal to ensure the signal is scoped to the asynchronous call context
    private static readonly AsyncLocal<ReplaySubject<int>> ExitSubject = new();


    /// <summary>
    /// Creates an observable that immediately terminates with an exit signal, propagating the specified
    /// exit code and message to observers as an error.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence. The value is never emitted since the observable throws immediately.</typeparam>
    /// <param name="exitCode">The exit code to be included in the exit signal.</param>
    /// <param name="message">The message describing the reason for the exit.</param>
    /// <returns>An observable sequence that terminates with an <see cref="ExitException"/> containing the specified exit code and message.</returns>
    /// <remarks>
    /// This method is typically used to propagate an exit signal through an observable sequence in a reactive programming context. 
    /// The sequence immediately throws an <see cref="ExitException"/>, so no values are emitted.
    /// </remarks>
    /// <exception cref="ExitException">Always throws an <see cref="ExitException"/> with the specified exit code and message.</exception>
    [DebuggerNonUserCode]
    public static IObservable<T> AsObservable<T>(int exitCode, string message) => System.Reactive.Linq.Observable.Throw<T>(new ExitException(exitCode, message));

    [DebuggerNonUserCode]
    public static IObservable<T> AsObservable<T>(string message) => System.Reactive.Linq.Observable.Throw<T>(new ExitException(1, message));


    /// <summary>
    /// Triggers an exit signal with an error code of 1 and the provided message.
    /// </summary>
    /// <param name="message">Exit message</param>
    /// <returns>An ExitException with exit code 1</returns>
    [DebuggerStepThrough]
    public static Exception With(string message) => With(1, message);

    /// <summary>
    /// Triggers an exit signal with the provided error code and message.
    /// </summary>
    /// <param name="exitCode">Exit code</param>
    /// <param name="message">Exit message</param>
    /// <returns>An ExitException with the provided exit code</returns>
    public static Exception With(int exitCode, string message)
    {
        var exception = new ExitException(exitCode, message);
        var subject = ExitSubject.Value;

        // Ensure a subject exists to observe the signal
        if (subject is null)
        {
            throw exception; // If no subject is present, immediately throw
        }

        // Notify subscribers of the exit signal and error
        subject.AsObserver().OnError(exception);

        // Debugging hook for developers to notice when the exit is triggered
        Debug.Fail($"Exit triggered with code {exitCode} and message: {message}");

        return exception;
    }

    /// <summary>
    /// Determines whether the provided exception is an exit signal.
    /// </summary>
    /// <param name="exception">The exception to inspect</param>
    /// <param name="exitCode">Outputs the exit code if the exception is an exit signal</param>
    /// <param name="message">Outputs the exit message if the exception is an exit signal</param>
    /// <returns>True if the exception is an exit signal; otherwise, false</returns>
    internal static bool IsMatch(Exception exception, out int exitCode, out string message)
    {
        if (exception is ExitException exit)
        {
            exitCode = exit.ExitCode;
            message = exit.Message;
            return true;
        }

        // Default out values
        exitCode = 0;
        message = string.Empty;
        return false;
    }


    internal static int WithCode(Func<int> callback)
    {
        // Ensure the ReplaySubject exists, or create a new one with a buffer size of 1
        var subject = ExitSubject.Value ??= new ReplaySubject<int>(1);

        // Return an observable that runs the callback and notifies subscribers
        return Observable
            .Create<int>(observer =>
            {
                // Subscribe to the subject and forward emissions to the observer
                var subscription = subject.AsObservable().Subscribe(observer);

                try
                {
                    // Invoke the callback and signal the exit code
                    var exitCode = callback.Invoke();
                    subject.AsObserver().OnNext(exitCode);
                    subject.OnCompleted(); // Signal that the exit code has been provided
                }
                catch (Exception ex)
                {
                    // If an exception occurs, propagate it as an error
                    subject.OnError(ex);
                }

                // Return a disposable to clean up the subscription
                return Disposable.Create(() =>
                {
                    subscription.Dispose();
                    subject.Dispose(); // Dispose of the ReplaySubject to free resources
                });
            })
            .ToTask()
            .GetAwaiter()
            .GetResult();
    }
}



public static partial class CliExit
{
    internal static Exception DictionaryOptionValueParseFailure(string option, string key, Type valueType)
    {
        return With(
            $"Invalid input for option '{option}', key '{key}': the specified value could not be parsed to '{valueType.FullName}'.");
    }

    internal static Exception InvalidDictionaryOptionKeyValueInput(string option, string capture)
    {
        return With(
            $"Invalid input for option '{option}'. " +
            $"Expected a key-value pair but received '{capture}'. Please provide both a key and a value.");
    }

    internal static Exception ConflictingOptionValues(string option)
    {
        return With(
            $"The option '{option}' was specified with conflicting values. Specify a single value for this option and try again.");
    }

    internal static Exception InvalidOptionInputParsing(string option, Type valueType)
    {
        return With(
            $"The input for the option '{option}' is invalid. " +
            $"The value could not be parsed to the expected type '{valueType.FullName}'. " +
            "Please provide a valid input and try again.");
    }

    internal static Exception CollectionOptionParsingFailure(string option, Type itemType)
    {
        return With(
            $"Invalid input for the option '{option}': the provided value could not be parsed to the expected collection item type '{itemType.FullName}'. " +
            "Ensure the input is in the correct format and try again.");
    }

    internal static Exception DictionaryKeyMissingValue(string option, Group keyGroup)
    {
        return With(
            $"The dictionary key '{keyGroup.Value}' for the option '{option}' is missing a corresponding value. " +
            "Please provide a value for the key and try again.");
    }

    internal static Exception DictionaryValueMissingKey(string option, Group valueGroup)
    {
        return With(
            $"A key is missing for the value '{valueGroup.Value}' in the dictionary option '{option}'. " +
            "Please provide a corresponding key for this value and try again.");
    }

    internal static Exception MissingRequiredOption(CliOptionInfo option)
    {
        return With($"The required option '{option.AliasPipeExpression}' is missing. Please provide this option and try again.");
    }

}
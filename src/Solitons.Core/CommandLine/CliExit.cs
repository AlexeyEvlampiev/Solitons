using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace Solitons.CommandLine;

/// <summary>
/// Provides functionality to manage exit signals within asynchronous call chains, allowing for 
/// graceful handling of exit codes and error messages in an observable manner.
/// </summary>
public static class CliExit
{
    [method: DebuggerNonUserCode]
    sealed class ExitException(int exitCode, string message) : Exception(message)
    {
        public int ExitCode { get; } = exitCode;
    }

    // AsyncLocal to ensure the signal is scoped to the asynchronous call context
    private static readonly AsyncLocal<ReplaySubject<int>> ExitSignal = new();

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
        var subject = ExitSignal.Value;

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
    internal static bool IsExitSignal(Exception exception, out int exitCode, out string message)
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

    /// <summary>
    /// Creates an observable that signals the exit code returned by the callback function.
    /// </summary>
    /// <param name="callback">A function that returns the exit code</param>
    /// <returns>An observable of the exit code</returns>
    internal static IObservable<int> From(Func<int> callback)
    {
        // Ensure the ReplaySubject exists, or create a new one with a buffer size of 1
        var subject = ExitSignal.Value ??= new ReplaySubject<int>(1);

        // Return an observable that runs the callback and notifies subscribers
        return Observable.Create<int>(observer =>
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
        });
    }
}

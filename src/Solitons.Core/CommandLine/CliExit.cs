using System;
using System.Diagnostics;
using System.Reactive.Linq;

namespace Solitons.CommandLine;

/// <summary>
/// Provides methods to terminate the CLI application with a specific exit code and error message.
/// </summary>
public static class CliExit
{
    /// <summary>
    /// Terminates the application by throwing a <see cref="CliExitException"/> with the specified exit code and error message.
    /// </summary>
    /// <param name="exitCode">The exit code to return to the operating system.</param>
    /// <param name="message">The error message to include in the exception.</param>
    /// <exception cref="CliExitException">Thrown to terminate the application with the specified exit code.</exception>
    [DebuggerNonUserCode]
    public static void With(int exitCode, string message)
    {
        throw new CliExitException(message)
        {
            ExitCode = exitCode
        };
    }

    /// <summary>
    /// Terminates the application by throwing a <see cref="CliExitException"/> with a default exit code of 1 and the specified error message.
    /// </summary>
    /// <param name="message">The error message to include in the exception.</param>
    /// <exception cref="CliExitException">Thrown to terminate the application with an exit code of 1.</exception>
    [DebuggerNonUserCode]
    public static void With(string message)
    {
        throw new CliExitException(message)
        {
            ExitCode = 1
        };
    }

    /// <summary>
    /// Returns an observable that throws a <see cref="CliExitException"/> with the specified exit code and error message when subscribed to.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence.</typeparam>
    /// <param name="exitCode">The exit code to return to the operating system.</param>
    /// <param name="message">The error message to include in the exception.</param>
    /// <returns>An observable that throws a <see cref="CliExitException"/>.</returns>
    [DebuggerNonUserCode]
    public static IObservable<T> AsObservable<T>(int exitCode, string message)
    {
        return Observable.Throw<T>(new CliExitException(message)
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
    public static IObservable<T> AsObservable<T>(string message)
    {
        return Observable.Throw<T>(new CliExitException(message)
        {
            ExitCode = 1
        });
    }

    /// <summary>
    /// Determines whether the specified exception is a <see cref="CliExitException"/>.
    /// </summary>
    /// <param name="e">The exception to evaluate.</param>
    /// <returns>
    /// <c>true</c> if the exception is a <see cref="CliExitException"/>; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Use this method to identify exceptions that are intended to terminate the CLI application 
    /// with a specific exit code. This can be particularly useful in scenarios where you need 
    /// to distinguish between application termination exceptions and other types of exceptions.
    /// </remarks>
    [DebuggerNonUserCode]
    public static bool IsTerminationRequest(Exception e) => e is CliExitException;

}
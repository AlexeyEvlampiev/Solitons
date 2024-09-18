using System;
using System.Diagnostics;
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
public partial class CliExit : IDisposable
{
    private readonly Action<CliExitException> _onExitCallback;
    private readonly IObservable<int> _exitSignalStream;
    private readonly IDisposable _cleanupDisposable;
    private readonly CancellationTokenSource _cancellationSource;
    private static readonly AsyncLocal<CliExit> CurrentExitContext = new();

    private CliExit()
    {
        var exitSubject= new ReplaySubject<int>(1);
        _cancellationSource = new CancellationTokenSource();
        _onExitCallback = (exitException) =>
        {
            var cancel = _cancellationSource.CancelAsync();
            exitSubject.OnError(exitException);
            cancel.Wait();

        };
        _exitSignalStream = exitSubject.AsObservable();
        _cleanupDisposable = Disposable.Create(() =>
        {
            exitSubject.OnCompleted();
            exitSubject.Dispose();
        });
    }

    [DebuggerStepThrough]
    private void Raise(CliExitException exception)
    {
        try
        {
            _onExitCallback.Invoke(exception);
        }
        catch (Exception ex)
        {
            Debug.Fail($"Failed to handle exit: {ex.Message}");
        }
    }

    private IDisposable SubscribeForExitEvents(IObserver<int> observer)
    {
        return _exitSignalStream.Subscribe(
            onNext: _ => Debug.Fail("Unexpected OnNext event. Exits should only propagate as errors."),
            onError: observer.OnError,
            onCompleted: observer.OnCompleted 
        );
    }

    /// <summary>
    /// Creates an observable that immediately terminates with an exit signal, propagating the specified
    /// exit code and message to observers as an error.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence. The value is never emitted since the observable throws immediately.</typeparam>
    /// <param name="exitCode">The exit code to be included in the exit signal.</param>
    /// <param name="message">The message describing the reason for the exit.</param>
    /// <returns>An observable sequence that terminates with an <see cref="CliExitException"/> containing the specified exit code and message.</returns>
    /// <remarks>
    /// This method is typically used to propagate an exit signal through an observable sequence in a reactive programming context. 
    /// The sequence immediately throws an <see cref="CliExitException"/>, so no values are emitted.
    /// </remarks>
    /// <exception cref="CliExitException">Always throws an <see cref="CliExitException"/> with the specified exit code and message.</exception>
    [DebuggerNonUserCode]
    public static IObservable<T> AsObservable<T>(int exitCode, string message) => System.Reactive.Linq.Observable.Throw<T>(new CliExitException(exitCode, message));

    /// <summary>
    /// Creates an observable that terminates with an exit signal by throwing an <see cref="CliExitException"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements in the observable sequence. No elements will be emitted.</typeparam>
    /// <param name="message">A message describing the reason for the exit.</param>
    /// <returns>An observable sequence that terminates with a <see cref="CliExitException"/>.</returns>
    /// <example>
    /// <code>
    /// var exitObservable = CliExit.AsObservable&lt;int&gt;(2, "Unexpected exit");
    /// exitObservable.Subscribe(
    ///     _ => {}, 
    ///     ex => Console.WriteLine($"Error: {ex.Message}"));
    /// </code>
    /// </example>
    /// <exception cref="CliExitException">Always throws an exception with the provided exit code and message.</exception>

    [DebuggerNonUserCode]
    public static IObservable<T> AsObservable<T>(string message) => System.Reactive.Linq.Observable.Throw<T>(new CliExitException(1, message));


    /// <summary>
    /// Triggers an exit signal with an error code of 1 and the provided message.
    /// </summary>
    /// <param name="message">Exit message</param>
    /// <returns>An ExitException with exit code 1</returns>
    [DebuggerStepThrough]
    public static Exception Raise(string message) => Raise(1, message);

    /// <summary>
    /// Triggers an exit signal with the provided error code and message.
    /// </summary>
    /// <param name="exitCode">Exit code</param>
    /// <param name="message">Exit message</param>
    /// <returns>An ExitException with the provided exit code</returns>
    public static Exception Raise(int exitCode, string message)
    {
        var exception = new CliExitException(exitCode, message);
        var exit = CurrentExitContext.Value;
        if (exit is null)
        {
            throw exception;
        }

        // Notify subscribers of the exit signal and error
        exit._onExitCallback(exception);

        // Debugging hook for developers to notice when the exit is triggered
        Debug.Fail($"Exit triggered with code {exitCode} and message: {message}");

        return exception;
    }


    internal static IObservable<int> AsObservable(Func<int> callback)
    {
        var exit = (CurrentExitContext.Value = new CliExit());

        return Observable
            .Create<int>(observer =>
            {
                using var exitListener = exit.SubscribeForExitEvents(observer);
                try
                {
                    var exitCode = callback.Invoke();
                    observer.OnNext(exitCode);
                    observer.OnCompleted();
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }

                return exit;
            });
    }

    internal static int Using(Func<int> callback)
    {
        return AsObservable(callback)
            .Catch((CliExitException e) =>
            {
                if (e.ExitCode != 0)
                {
                    Console.Error.WriteLine(e.Message);
                }
                else
                {
                    Console.WriteLine(e.Message);
                }
                return Observable.Return(e.ExitCode);
            })
            .Catch((Exception e) =>
            {
                Console.Error.WriteLine("Internal error");
                return Observable.Return(1);
            })
            .FirstOrDefaultAsync()
            .ToTask()
            .GetAwaiter()
            .GetResult();
    }

    public static CancellationToken GetCancellationToken()
    {
        var exit = CurrentExitContext.Value;
        if (exit is null)
        {
            return default;
        }

        return exit._cancellationSource.Token;
    }

    [DebuggerStepThrough]
    void IDisposable.Dispose() => _cleanupDisposable.Dispose();
}

public partial class CliExit
{
    internal static Exception DictionaryOptionValueParseFailure(string option, string key, Type valueType)
    {
        return Raise(
            $"Invalid input for option '{option}', key '{key}': the specified value could not be parsed to '{valueType.FullName}'.");
    }

    internal static Exception InvalidDictionaryOptionKeyValueInput(string option, string capture)
    {
        return Raise(
            $"Invalid input for option '{option}'. " +
            $"Expected a key-value pair but received '{capture}'. Please provide both a key and a value.");
    }

    internal static Exception ConflictingOptionValues(string option)
    {
        return Raise(
            $"The option '{option}' was specified with conflicting values. Specify a single value for this option and try again.");
    }

    internal static Exception InvalidOptionInputParsing(string option, Type valueType)
    {
        return Raise(
            $"The input for the option '{option}' is invalid. " +
            $"The value could not be parsed to the expected type '{valueType.FullName}'. " +
            "Please provide a valid input and try again.");
    }

    internal static Exception CollectionOptionParsingFailure(string option, Type itemType)
    {
        return Raise(
            $"Invalid input for the option '{option}': the provided value could not be parsed to the expected collection item type '{itemType.FullName}'. " +
            "Ensure the input is in the correct format and try again.");
    }

    internal static Exception DictionaryKeyMissingValue(string option, Group keyGroup)
    {
        return Raise(
            $"The dictionary key '{keyGroup.Value}' for the option '{option}' is missing a corresponding value. " +
            "Please provide a value for the key and try again.");
    }

    internal static Exception DictionaryValueMissingKey(string option, Group valueGroup)
    {
        return Raise(
            $"A key is missing for the value '{valueGroup.Value}' in the dictionary option '{option}'. " +
            "Please provide a corresponding key for this value and try again.");
    }

    internal static Exception MissingRequiredOption(CliOptionInfo option)
    {
        return Raise($"The required option '{option.AliasPipeExpression}' is missing. Please provide this option and try again.");
    }

}
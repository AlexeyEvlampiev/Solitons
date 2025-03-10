﻿using System;
using System.Reactive;
using System.Reactive.Linq;

namespace Solitons.Reactive;

/// <summary>
/// Arguments supplied to the Retry Policy Handler.
/// </summary>
public sealed class RetryTrigger : ObservableBase<Unit>
{
    /// <summary>
    /// Initializes a new instance of the RetryPolicyArgs class.
    /// </summary>
    /// <param name="exception">The exception that caused the retry.</param>
    /// <param name="attemptNumber">The number of the current attempt.</param>
    /// <param name="firstAttemptTime">The time of the first attempt.</param>
    internal RetryTrigger(Exception exception, int attemptNumber, DateTimeOffset firstAttemptTime)
    {
        Exception = exception;
        AttemptNumber = attemptNumber;
        FirstAttemptTime = firstAttemptTime;
        ElapsedTimeSinceFirstException = (DateTimeOffset.UtcNow - firstAttemptTime);
    }

    /// <summary>
    /// Gets the exception that caused the retry.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Gets the number of the current attempt.
    /// </summary>
    public int AttemptNumber { get; }

    /// <summary>
    /// Gets the time of the first attempt.
    /// </summary>
    public DateTimeOffset FirstAttemptTime { get; }

    /// <summary>
    /// Gets the elapsed time since the first exception.
    /// </summary>
    public TimeSpan ElapsedTimeSinceFirstException { get; }

    protected override IDisposable SubscribeCore(IObserver<Unit> observer) => Observable
        .Return(Unit.Default)
        .Subscribe(observer);
}

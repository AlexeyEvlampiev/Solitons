using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Solitons.Diagnostics.Common;

namespace Solitons.Diagnostics;

/// <summary>
/// An abstract base class for implementing an asynchronous logger with buffering capabilities.
/// This logger buffers log messages based on their severity level and processes them in batches.
/// </summary>
public abstract class BufferedAsyncLogger : AsyncLogger
{
    /// <summary>
    /// Immutable record to hold the configuration for buffering log messages.
    /// </summary>
    /// <param name="MaxBufferDuration">The maximum time duration for buffering log messages before they are processed.</param>
    /// <param name="MaxBufferSize">The maximum number of log messages to buffer before they are processed.</param>
    sealed record BufferConfig(TimeSpan MaxBufferDuration, int MaxBufferSize);
    private readonly IObserver<LogEventArgs> _observer;
    private readonly Dictionary<LogLevel, BufferConfig> _bufferConfig = new();
    private readonly IDisposable _subscription;

    /// <summary>
    /// Initializes a new instance of the <see cref="BufferedAsyncLogger"/> class.
    /// </summary>
    /// <param name="config">An action that configures the buffering options for the logger.</param>
    protected BufferedAsyncLogger(Action<Options> config)
    {
        _bufferConfig[LogLevel.Error] = new BufferConfig(TimeSpan.Zero, 1);
        _bufferConfig[LogLevel.Warning] = new BufferConfig(TimeSpan.FromMilliseconds(100), 100);
        _bufferConfig[LogLevel.Info] = new BufferConfig(TimeSpan.FromMilliseconds(300), 300);

        config.Invoke(new Options(this));

        var subject = new Subject<LogEventArgs>();
        _observer = subject.AsObserver();
        _subscription = subject
            .AsObservable()
            .GroupBy(log => log.Level)
            .SelectMany(group =>
            {
                if (_bufferConfig.TryGetValue(group.Key, out var info))
                {
                    return group.Buffer(info.MaxBufferDuration, info.MaxBufferSize);
                }

                return group.Buffer(TimeSpan.Zero, 1);
            })
            .Where(buffer => buffer.Count > 0)
            .Subscribe(buffer => LogAsync(buffer), e => Trace.TraceError(e.ToString()));
    }

    /// <summary>
    /// Processes a batch of buffered log messages asynchronously.
    /// Implementations of this method should handle the actual logging mechanism.
    /// </summary>
    /// <param name="args">The list of buffered <see cref="LogEventArgs"/> to process.</param>
    /// <returns>A task that represents the asynchronous logging operation.</returns>
    protected abstract Task LogAsync(IList<LogEventArgs> args);

    /// <summary>
    /// Logs a single event, which is buffered before being processed.
    /// </summary>
    /// <param name="args">The log event arguments to be logged.</param>
    /// <returns>A completed task.</returns>
    [DebuggerStepThrough]
    protected sealed override Task LogAsync(LogEventArgs args)
    {
        _observer.OnNext(args);
        return Task.CompletedTask;
    }


    /// <summary>
    /// Nested class for configuring buffer settings for the logger.
    /// </summary>
    public sealed class Options
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly BufferedAsyncLogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="Options"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="BufferedAsyncLogger"/> instance to configure.</param>
        internal Options(BufferedAsyncLogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Configures buffering for error-level log messages.
        /// </summary>
        /// <param name="maxBufferDuration">The maximum time duration for buffering error-level log messages.</param>
        /// <param name="maxBufferSize">The maximum number of error-level log messages to buffer.</param>
        /// <returns>The current instance of <see cref="Options"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="maxBufferDuration"/> is negative or if <paramref name="maxBufferSize"/> is less than 1.
        /// </exception>
        public Options BufferErrors(TimeSpan maxBufferDuration, int maxBufferSize)
        {
            if (maxBufferDuration < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(maxBufferDuration), "maxBufferDuration cannot be negative.");
            if (maxBufferSize < 1)
                throw new ArgumentOutOfRangeException(nameof(maxBufferSize), "maxBufferSize must be at least 1.");

            _logger._bufferConfig[LogLevel.Error] = new BufferConfig(maxBufferDuration, maxBufferSize);
            return this;
        }

        /// <summary>
        /// Configures buffering for warning-level log messages.
        /// </summary>
        /// <param name="maxBufferDuration">The maximum time duration for buffering warning-level log messages.</param>
        /// <param name="maxBufferSize">The maximum number of warning-level log messages to buffer.</param>
        /// <returns>The current instance of <see cref="Options"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="maxBufferDuration"/> is negative or if <paramref name="maxBufferSize"/> is less than 1.
        /// </exception>
        public Options BufferWarnings(TimeSpan maxBufferDuration, int maxBufferSize)
        {
            if (maxBufferDuration < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(maxBufferDuration), "maxBufferDuration cannot be negative.");
            if (maxBufferSize < 1)
                throw new ArgumentOutOfRangeException(nameof(maxBufferSize), "maxBufferSize must be at least 1.");

            _logger._bufferConfig[LogLevel.Warning] = new BufferConfig(maxBufferDuration, maxBufferSize);
            return this;
        }

        /// <summary>
        /// Configures buffering for info-level log messages.
        /// </summary>
        /// <param name="maxBufferDuration">The maximum time duration for buffering info-level log messages.</param>
        /// <param name="maxBufferSize">The maximum number of info-level log messages to buffer.</param>
        /// <returns>The current instance of <see cref="Options"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="maxBufferDuration"/> is negative or if <paramref name="maxBufferSize"/> is less than 1.
        /// </exception>
        public Options BufferInfo(TimeSpan maxBufferDuration, int maxBufferSize)
        {
            if (maxBufferDuration < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(maxBufferDuration), "maxBufferDuration cannot be negative.");
            if (maxBufferSize < 1)
                throw new ArgumentOutOfRangeException(nameof(maxBufferSize), "maxBufferSize must be at least 1.");

            _logger._bufferConfig[LogLevel.Info] = new BufferConfig(maxBufferDuration, maxBufferSize);
            return this;
        }
    }
}
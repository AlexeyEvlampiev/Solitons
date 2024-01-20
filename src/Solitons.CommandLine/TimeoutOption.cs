using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

namespace Solitons.CommandLine;

/// <summary>
/// Represents a command line option for specifying an execution timeout, parsed as a <see cref="CancellationToken"/>.
/// </summary>
public sealed class TimeoutOption : ParseableOption<CancellationToken>
{
    /// <summary>
    /// The default timeout in seconds (1 hour).
    /// </summary>
    public const int DefaultTimeoutInSeconds = 60 * 60;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeoutOption"/> class with a default timeout descriptor.
    /// </summary>
    public TimeoutOption() 
        : base(new TimeoutOptionDescriptor())
    {

    }

    /// <summary>
    /// Gets the parsed result for this option from the <see cref="InvocationContext"/>.
    /// </summary>
    /// <param name="context">The invocation context of the command line parser.</param>
    /// <returns>A <see cref="CancellationToken"/> representing the specified timeout.</returns>
    public override CancellationToken GetParseResult(InvocationContext context)
    {
        return base
            .GetParseResult(context)
            .Join(context.GetCancellationToken());
    }

    /// <summary>
    /// Provides a descriptor for the timeout option, parsing the argument into a <see cref="CancellationToken"/>.
    /// </summary>
    sealed class TimeoutOptionDescriptor : OptionDescriptor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimeoutOptionDescriptor"/> class.
        /// </summary>
        public TimeoutOptionDescriptor() 
            : base("--timeout", true, "Execution timeout")
        {
        }

        /// <summary>
        /// Parses the command line argument into a <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="result">The result of the command line argument parsing.</param>
        /// <returns>A <see cref="CancellationToken"/> representing the specified timeout.</returns>
        /// <remarks>
        /// If no value is specified, a default timeout of one hour is used. 
        /// The timeout can be specified in seconds or as a standard TimeSpan format.
        /// </remarks>
        public override CancellationToken Parse(ArgumentResult result)
        {
            if (result.Tokens.Count == 0)
            {
                return new CancellationTokenSource(DefaultTimeoutInSeconds * 1000).Token;
            }

            var token = result.Tokens[0].Value;
            if (int.TryParse(token, out var seconds))
            {
                if (seconds > 0)
                {
                    return new CancellationTokenSource(seconds * 1000).Token;
                }

                result.ErrorMessage = $"The specified timeout value should be expressed as a positive integer representing a valid number of seconds.";
                return CancellationToken.None;
            }

            if (TimeSpan.TryParse(token, out var timeout))
            {
                if (timeout > TimeSpan.Zero)
                {
                    return new CancellationTokenSource(timeout).Token;
                }

                result.ErrorMessage = $"The timeout value specified must be a positive duration.";
                return CancellationToken.None;
            }

            result.ErrorMessage = $"The specified timeout value must be expressed as a positive duration measured in seconds or a valid timespan expression.";
            return CancellationToken.None;
        }
    }

}
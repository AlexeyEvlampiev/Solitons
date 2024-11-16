using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Solitons.Text.RegularExpressions;

namespace Solitons.CommandLine;

/// <summary>
/// Represents a parsed command line with executable name, arguments, and options.
/// </summary>
/// <remarks>
/// The <see cref="CliCommandLine"/> class provides a strongly typed structure for handling command line inputs.
/// It parses a given command line string into its constituent parts, including the executable name, subcommands,
/// arguments, and various options. This class ensures that the command line adheres to the expected format
/// and throws descriptive exceptions if any discrepancies are found.
/// </remarks>
public sealed class CliCommandLine : IFormattable
{
    private delegate string Transformer(string commandLine, ParsingContext context);

    /// <summary>
    /// Parses the specified command line string and returns a <see cref="CliCommandLine"/> instance representing its components.
    /// </summary>
    /// <param name="commandLine">The raw command line string to parse.</param>
    /// <returns>
    /// A <see cref="CliCommandLine"/> object that contains the executable name, arguments, and options extracted from the provided command line.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the <paramref name="commandLine"/> is <c>null</c>, empty, or consists solely of white-space characters.
    /// </exception>
    /// <exception cref="CliCommandLineFormatException">
    /// Thrown when the command line format is invalid or when the executable name cannot be extracted properly.
    /// </exception>
    /// <remarks>
    /// The <see cref="Parse"/> method processes the input command line string through a series of transformation steps to extract the executable name,
    /// environment variables, quoted strings, keyed options, and other options. It ensures that the command line adheres to the expected format
    /// and encodes specific components to maintain consistency and avoid conflicts during parsing.
    /// 
    /// This method is marked with the <see cref="DebuggerStepThroughAttribute"/> attribute to instruct the debugger to step through the method without
    /// stopping, providing a smoother debugging experience by avoiding stepping into boilerplate code.
    /// </remarks>
    [DebuggerStepThrough]
    public static CliCommandLine Parse(string commandLine) => ThrowIf
            .ArgumentNullOrWhiteSpace(commandLine)
            .Trim()
            .Convert(text => new CliCommandLine(text));

    private CliCommandLine(string originalCommand)
    {
        CommandLine = originalCommand;
        var context = new ParsingContext(originalCommand);

        var transformers = new Transformer[]
        {
            ProcessCommandName,
            ProcessEnvironmentVariables,
            ProcessQuotedStrings,
            FormatKeyedOptions,
            CaptureSegments,
            ExtractOptions
        };

        foreach (var transformer in transformers)
        {
            originalCommand = transformer.Invoke(originalCommand, context);
        }

        Signature = originalCommand.Trim();
        ExecutableName = ThrowIf.NullOrWhiteSpace(context.ExecutableName).Trim();
        Segments = [..context.Segments];
        Options = [..context.Options];
    }



    /// <summary>
    /// Gets the name of the executable extracted from the command line.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> representing the executable name.
    /// </value>
    public string ExecutableName { get; }

    /// <summary>
    /// Gets the original command line string that was parsed.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> representing the raw command line input.
    /// </value>
    public string CommandLine { get; }

    /// <summary>
    /// Gets the normalized signature of the command line, used for handler selection.
    /// </summary>
    /// <remarks>
    /// The <see cref="Signature"/> property represents a simplified and standardized version of the original
    /// command line. It strips away unnecessary details and normalizes subcommands, arguments, and options
    /// to create a consistent format. This normalized signature is instrumental in selecting the most
    /// appropriate handler by matching it against predefined regular expressions associated with each handler.
    /// By using the <see cref="Signature"/>, the system ensures accurate and efficient handler matching,
    /// enhancing both performance and reliability.
    /// </remarks>
    /// <value>
    /// A <see cref="string"/> containing the normalized command line signature used for matching handlers.
    /// </value>
    public string Signature { get; }

    /// <summary>
    /// Gets a collection of parsed options extracted from the command line.
    /// </summary>
    /// <value>
    /// An <see cref="ImmutableArray{T}"/> of <see cref="CliOptionCapture"/> representing the options
    /// extracted from the command line. These options can include flags, key-value pairs, or collections of values.
    /// </value>
    public ImmutableArray<CliOptionCapture> Options { get; }

    /// <summary>
    /// Gets a collection of subcommands or arguments captured from the command line.
    /// </summary>
    /// <value>
    /// An <see cref="ImmutableArray{T}"/> of <see cref="string"/> representing the segments
    /// (subcommands or arguments) extracted from the command line.
    /// </value>
    /// <remarks>
    /// The <see cref="Segments"/> property contains the subcommands or arguments that follow the executable name.
    /// These segments provide additional context or instructions for the command being executed.
    /// </remarks>
    public ImmutableArray<string> Segments { get; }

    /// <summary>
    /// Returns a string representation of the command line based on the specified format.
    /// </summary>
    /// <param name="format">
    /// The format specifier. Use <c>null</c> or "G" for the original command line string,
    /// and "S" or "Signature" for the normalized signature.
    /// </param>
    /// <param name="formatProvider">
    /// The format provider to use. This parameter is ignored in the current implementation.
    /// </param>
    /// <returns>
    /// A <see cref="string"/> representation of the command line based on the specified format.
    /// </returns>
    /// <exception cref="FormatException">
    /// Thrown when an unsupported format specifier is provided.
    /// </exception>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        if (string.IsNullOrEmpty(format) || string.Equals(format, "G", StringComparison.OrdinalIgnoreCase))
        {
            return CommandLine;
        }

        if (string.Equals(format, "S", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(format, "Signature", StringComparison.OrdinalIgnoreCase))
        {
            return Signature;
        }

        throw new FormatException($"The format string '{format}' is not supported.");
    }

    /// <summary>
    /// Returns a string representation of the command line using general formatting.
    /// </summary>
    /// <returns>
    /// A <see cref="string"/> representing the original command line.
    /// </returns>
    public override string ToString()
    {
        return ToString("G", CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Implicitly converts a <see cref="CliCommandLine"/> instance to a <see cref="string"/> representation.
    /// </summary>
    /// <param name="commandLine">The <see cref="CliCommandLine"/> instance to convert.</param>
    /// <returns>
    /// A <see cref="string"/> that represents the command line as a string.
    /// </returns>
    /// <remarks>
    /// This operator allows a <see cref="CliCommandLine"/> instance to be implicitly converted to a string
    /// by calling its <see cref="ToString"/> method, which returns the original command line.
    /// </remarks>
    public static implicit operator string(CliCommandLine commandLine) => commandLine.ToString();

    private static string ProcessCommandName(string commandLine, ParsingContext context)
    {
        var match = @"(?xis-m)^(?<executable>""[^""]+""|\S+)(?:\s+(?<input>.*))?$"
            .Convert(pattern => new Regex(pattern))
            .Match(commandLine.Trim());
        if (false == match.Success)
        {
            throw CliCommandLineFormatException.GeneralFormatMismatch();
        }

        var executable = match.Groups["executable"].Value.Trim('"');
        var input = match.Groups["input"].Value.Trim();
        try
        {
            executable = Path.GetFileName(executable);
        }
        catch (Exception e)
        {
            throw CliCommandLineFormatException.InvalidExecutableName(e);
        }

        context.ExecutableName = executable;

        if (RegexUtils.HasWhiteSpaces(executable))
        {
            var encoded = context.Encode(executable);
            return $"{encoded} {input}";
        }

        return $"{executable} {input}";
    }

    private string ProcessEnvironmentVariables(string commandline, ParsingContext context)
    {
        return @"%[\w_]+%"
            .Replace("$variable", @"%[\w_]+%")
            .Convert(pattern => new Regex(pattern))
            .Replace(commandline, match =>
            {

                var value = match.Value
                    .Trim('"')
                    .Trim('%');
                var envVariable = Environment.GetEnvironmentVariable(value);
                if (envVariable.IsPrintable())
                {
                    var key = context.Encode(envVariable!);
                    return key;

                }

                return match.Value;
            });
    }

    private string ProcessQuotedStrings(string commandLine, ParsingContext context)
    {
        return @"""[^""]*"""
            .Convert(pattern => new Regex(pattern))
            .Replace(commandLine, match =>
            {
                var value = match.Value.Trim('"');
                return context.Encode(value);
            });
    }

    private string FormatKeyedOptions(string commandline, ParsingContext context)
    {
        commandline = @"(?<option>-{1,}$option)\s*\[\s*(?<key>$key)\s*\]"
            .Replace("$option", @"[^\[\s]+")
            .Replace("$key", @"[^\[\]\s]+")
            .Convert(pattern => new Regex(pattern))
            .Replace(commandline, match => match.Result("${option}.${key}"));

        return commandline;
    }

    private string CaptureSegments(string commandline, ParsingContext context)
    {
        Debug.Assert(context.ExecutableName.IsPrintable());
        var segments = @"^(?<executable>\S+)\s+(?:(?<segment>[^-\s]\S*)\s*)*"
                .Convert(pattern => new Regex(pattern))
                .Match(commandline)
                .Convert(m => m.Groups["segment"].Captures)
                .Select(c => c.Value)
                .ToArray();
        context.RegisterSegments(segments);
        return commandline;
    }

    private string ExtractOptions(string commandline, ParsingContext context)
    {
        commandline = @"(?xis-m)(?<=\s|^)
            (?<option>(?<name>-{1,2}[^\.\s]+) (?:\.(?<key>\S*))?)   
            (?:\s+(?<value>[^-\s]\S*))*"
            .Convert(pattern => new Regex(pattern))
            .Replace(commandline, match =>
            {
                var option = match.Groups["option"];
                var name = match.Groups["name"];
                var key = match.Groups["key"];
                var values = match.Groups["value"].Captures.Select(c => c.Value).ToArray();
                Debug.Assert(option.Success);
                Debug.Assert(name.Success);
                if (key.Success)
                {
                    context.AddKeyedOption(name.Value, key.Value, values);
                }
                else
                {
                    context.AddOption(name.Value, values);
                }
                
                return name.Value;
            });
        return commandline;
    }

    sealed class ParsingContext(string commandLine)
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        private readonly Dictionary<string, string> _encodings = new(StringComparer.Ordinal);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string _commandLine = commandLine;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly List<CliOptionCapture> _options = new();
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _index = 0;

        public string ExecutableName { get; set; } = String.Empty;

        public string[] Segments { get; private set; } = [];

        public string Encode(string value)
        {
            var key = GenerateUniqueEncodingKey();
            _encodings.Add(key, value);
            return key;
        }

        private string Decode(string input)
        {
            if (_encodings.TryGetValue(input, out var decoded))
            {
                return decoded;
            }

            return input;
        }

        public ImmutableArray<CliOptionCapture> Options => [.._options];
        public IReadOnlyDictionary<string, string> Encodings => _encodings;

        private string GenerateUniqueEncodingKey()
        {
            var key = $"{{{_index++}}}";
            int attempt = 0;
            while (_commandLine.Contains(key))
            {
                key = $"{{{Guid.NewGuid():N}}}";
                if ((attempt++) > 10)
                {
                    throw new InvalidOperationException();
                }
            }

            return key;
        }

        public void AddKeyedOption(string name, string key, string[] values)
        {
            Debug.Assert(name.IsPrintable());
            Debug.Assert(key.IsPrintable());
            name = Decode(name);
            key = Decode(key);
            switch (values.Length)
            {
                case 0:
                    _options.Add(new CliKeyFlagOptionCapture(name, key));
                    break;
                case 1:
                    _options.Add(new CliKeyValueOptionCapture(name, key, values.Select(Decode).Single()));
                    break;
                default:
                    _options.Add(new CliKeyCollectionOptionCapture(name, key, [.. values.Select(Decode)]));
                    break;
            }
        }

        internal void AddOption(string name, string[] values)
        {
            Debug.Assert(name.IsPrintable());
            name = Decode(name);
            switch (values.Length)
            {
                case 0:
                    _options.Add(new CliFlagOptionCapture(name));
                    break;
                case 1:
                    _options.Add(new CliScalarOptionCapture(name, Decode(values[0])));
                    break;
                default:
                    _options.Add(new CliCollectionOptionCapture(name, [..values.Select(Decode)]));
                    break;
            }
        }

        public void RegisterSegments(string[] segments)
        {
            Debug.Assert(ExecutableName.IsPrintable());
            Debug.Assert(segments.Length == 0);
            Segments = segments.Select(Decode).ToArray();
        }
    }

}

/// <summary>
/// Represents an exception that is thrown when the format of a command line is invalid.
/// </summary>
/// <remarks>
/// This exception is used to indicate that there was an issue with parsing a command line string.
/// It provides detailed information about the format error, such as whether the executable name was invalid or 
/// if the command line format did not adhere to expected patterns.
/// </remarks>
public sealed class CliCommandLineFormatException : FormatException
{
    private CliCommandLineFormatException(string message) : base(message){ }

    private CliCommandLineFormatException(string message, Exception innerException) : base(message, innerException){}

    /// <summary>
    /// Creates a new <see cref="CliCommandLineFormatException"/> indicating a general format mismatch.
    /// </summary>
    /// <returns>A new instance of <see cref="CliCommandLineFormatException"/> with a general error message.</returns>
    internal static CliCommandLineFormatException GeneralFormatMismatch() => new(
        "The command line format is invalid. Expected format: [executable] [subcommands/arguments] [options].");

    /// <summary>
    /// Creates a new <see cref="CliCommandLineFormatException"/> indicating an invalid executable name.
    /// </summary>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    /// <returns>A new instance of <see cref="CliCommandLineFormatException"/> with a specific error message.</returns>
    internal static CliCommandLineFormatException InvalidExecutableName(Exception innerException) => new(
        "Failed to extract executable name.", innerException);
}

/// <summary>
/// Represents a captured command-line option, which can be a flag, value, or collection of values.
/// </summary>
/// <remarks>
/// The <see cref="CliOptionCapture"/> record serves as the base class for various specific types of command-line
/// options. Derived classes represent different option formats, such as flags, key-value pairs, or collections
/// of values. This structure allows for flexible handling of diverse command-line input patterns.
/// </remarks>
public abstract record CliOptionCapture
{
    protected internal CliOptionCapture(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the name of the command-line option.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> representing the name of the command-line option.
    /// </value>
    public string Name { get; }
}

/// <summary>
/// Represents a captured command-line flag option (e.g., <c>--verbose</c>).
/// </summary>
/// <remarks>
/// The <see cref="CliFlagOptionCapture"/> record represents a command-line option that acts as a flag.
/// It does not have a value associated with it but serves as a simple indicator that the option was specified.
/// This is used for boolean flags that are present or absent in the command line (e.g., <c>--verbose</c>).
/// </remarks>
public sealed record CliFlagOptionCapture(string Name) : CliOptionCapture(Name);

/// <summary>
/// Represents a captured command-line option that has a single value (e.g., <c>--output file.txt</c>).
/// </summary>
/// <remarks>
/// The <see cref="CliScalarOptionCapture"/> record represents a command-line option that has an associated
/// single value. This is typically used for options where a single argument follows the option (e.g., <c>--output file.txt</c>).
/// The `Value` property holds the associated value for the option.
/// </remarks>
public sealed record CliScalarOptionCapture(string Name, string Value) : CliOptionCapture(Name);

/// <summary>
/// Represents a captured command-line option that has multiple associated values (e.g., <c>--files file1.txt file2.txt</c>).
/// </summary>
/// <remarks>
/// The <see cref="CliCollectionOptionCapture"/> record represents a command-line option that can have multiple values.
/// This is typically used for options where multiple arguments follow the option (e.g., <c>--files file1.txt file2.txt</c>).
/// The `Values` property holds a collection of associated values for the option.
/// </remarks>
public sealed record CliCollectionOptionCapture(string Name, ImmutableArray<string> Values) : CliOptionCapture(Name);

/// <summary>
/// Represents a captured command-line option with a key and a flag (e.g., <c>--config[env]</c>).
/// </summary>
/// <remarks>
/// The <see cref="CliKeyFlagOptionCapture"/> record represents a command-line option that includes both a key and a flag.
/// This is typically used for options where the key is paired with a flag (e.g., <c>--config[env]</c>) without an associated value.
/// The `Key` property represents the key part of the option, while the `Name` property represents the option itself.
/// </remarks>
public sealed record CliKeyFlagOptionCapture(string Name, string Key) : CliOptionCapture(Name);

/// <summary>
/// Represents a captured command-line option with a key and a value (e.g., <c>--config[env] production</c>).
/// </summary>
/// <remarks>
/// The <see cref="CliKeyValueOptionCapture"/> record represents a command-line option that includes both a key and a value.
/// This is typically used for options where the key is paired with a value (e.g., <c>--config[env] production</c>).
/// The `Key` property holds the key part of the option, and the `Value` property holds the value associated with the key.
/// </remarks>
public sealed record CliKeyValueOptionCapture(string Name, string Key, string Value) : CliOptionCapture(Name);

/// <summary>
/// Represents a captured command-line option with a key and multiple associated values (e.g., <c>--config[env] test qa prod</c>).
/// </summary>
/// <remarks>
/// The <see cref="CliKeyCollectionOptionCapture"/> record represents a command-line option that includes both a key and a collection of values.
/// This is typically used for options where the key is paired with multiple values (e.g., <c>--config[env] test qa prod</c>).
/// The `Key` property holds the key part of the option (e.g., <c>env</c> in <c>--config[env]</c>), and the `Values` property holds the collection of values associated with the key (e.g., <c>test</c>, <c>qa</c>, and <c>prod</c>).
/// </remarks>
public sealed record CliKeyCollectionOptionCapture(string Name, string Key, ImmutableArray<string> Values) : CliOptionCapture(Name);
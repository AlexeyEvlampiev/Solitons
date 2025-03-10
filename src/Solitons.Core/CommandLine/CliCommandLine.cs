﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Solitons.Collections;
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
[DebuggerDisplay(@"{ToString(""D"")}")]
public sealed class CliCommandLine : IFormattable
{
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
    /// The <see cref="FromArgs"/> method processes the input command line string through a series of transformation steps to extract the executable name,
    /// environment variables, quoted strings, keyed options, and other options. It ensures that the command line adheres to the expected format
    /// and encodes specific components to maintain consistency and avoid conflicts during parsing.
    /// 
    /// This method is marked with the <see cref="DebuggerStepThroughAttribute"/> attribute to instruct the debugger to step through the method without
    /// stopping, providing a smoother debugging experience by avoiding stepping into boilerplate code.
    /// </remarks>
    [DebuggerStepThrough]
    public static CliCommandLine FromArgs(string commandLine)
    {
        commandLine = ThrowIf
            .ArgumentNullOrWhiteSpace(commandLine)
            .Trim();
        if (Regex.IsMatch(commandLine, @"^""[^""]*""$"))
        {
            return new CliCommandLine([commandLine.Trim('"')]);
        }

        return new CliCommandLine(SplitCommandLine(commandLine).ToArray());
    }


    [DebuggerStepThrough]
    public static CliCommandLine FromArgs() => new(Environment.GetCommandLineArgs());

    private CliCommandLine(string[] args)
    {
        var queue = new Queue<string>(args);
        ExecutableName = queue
            .Dequeue()
            .Trim('"')
            .Convert(path => path.Replace('\\', Path.DirectorySeparatorChar))
            .Convert(Path.GetFileName)!;
        Segments = [
            ..queue
                .DequeueWhile(arg => false == arg.StartsWith("-"))
                .Select(arg => arg.Trim('"'))
        ];
        var options = new List<CliOptionCapture>();
        var values = new List<string>();
        while (queue.Any())
        {
            var optionName = queue.Dequeue();
            optionName = Regex.Replace(optionName, @"\[([^\[\]]+)\]$", m => $@".{m.Groups[1]}");
            values.Clear();
            values.AddRange(queue
                .DequeueWhile(arg => false == arg.StartsWith("-"))
                .Select(v => v.Trim('"')));
            var match = Regex.Match(optionName, @"(?<option>\S+?)\.(?<key>[^\.]+)$");
            if (match.Success)
            {
                optionName = match.Groups["option"].Value;
                var key = match.Groups["key"].Value;
                if (values.Count == 0)
                {
                    options.Add(new CliKeyFlagOptionCapture(optionName, key));
                }
                else if (values.Count == 1)
                {
                    options.Add(new CliKeyValueOptionCapture(optionName, key, values.Single()));
                }
                else
                {
                    options.Add(new CliKeyCollectionOptionCapture(optionName, key, [.. values]));
                }
            }
            else if(values.Count == 0)
            {
                options.Add(new CliFlagOptionCapture(optionName));
            }
            else if (values.Count == 1)
            {
                options.Add(new CliScalarOptionCapture(optionName, values.Single()));
            }
            else
            {
                options.Add(new CliCollectionOptionCapture(optionName, [.. values]));
            }
        }

        Options = [.. options];
        CommandLine = ToString("g");
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

    internal static IEnumerable<string> SplitCommandLine(string commandLine)
    {
        var argRegex = new Regex(@"(?:(?<!\\)"".*?(?<!\\)""|\S+)");
        var envRegex = new Regex(@"(?is-m)%[a-z_]+%");
        for (Match match = argRegex.Match(commandLine); match.Success; match = match.NextMatch())
        {
            var arg = match.Value;
            arg = envRegex.Replace(arg, match =>
            {
                var key = match.Value.Trim('%');
                var value = Environment.GetEnvironmentVariable(key);
                if (value.IsPrintable())
                {
                    return value;
                }
                return match.Value;
            });
            yield return arg;
        }
    }

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
    public string ToString(string? format, IFormatProvider? formatProvider = null)
    {
        var builder = new StringBuilder(FluentList
            .Create(ExecutableName).AddRange(Segments)
            .Select(item => RegexUtils.HasWhiteSpaces(item) ? item.Quote() : item)
            .Join(" "));

        if (string.Equals(format, "D", StringComparison.OrdinalIgnoreCase))
        {
            Options
                .OfType<CliFlagOptionCapture>()
                .Select(o => o.Name.ToLower())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ForEach(o => builder.Append($" {o}"));

            Options
                .OfType<ICliCollectionCapture>()
                .Select(o => o.Name.ToLower())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ForEach(o => builder.Append($" {o} .."));

            Options
                .OfType<ICliMapOptionCapture>()
                .Select(o => o.Name.ToLower())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ForEach(o => builder.Append($" {o}[..] .."));
        }
        else
        {
            string QuoteIfHasWhiteSpaces(string value) => RegexUtils.HasWhiteSpaces(value) ? value.Quote() : value;
            Options
                .OfType<CliFlagOptionCapture>()
                .Select(o => o.Name.ToLower())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ForEach(o => builder.Append($" {o}"));

            Options
                .OfType<ICliCollectionCapture>()
                .GroupBy(o => o.Name.ToLowerInvariant(), o => o.Values, StringComparer.OrdinalIgnoreCase)
                .Select(grp => new
                {
                    Name = grp.Key,
                    Values = grp
                        .AsEnumerable()
                        .SelectMany(values => values)
                        .Select(QuoteIfHasWhiteSpaces)
                        .Join(" ")
                })
                .ForEach(o => builder.Append($" {o.Name} {o.Values}"));

            Options
                .OfType<CliKeyValueOptionCapture>()
                .ForEach(o => builder.Append($" {o.Name}[{QuoteIfHasWhiteSpaces(o.Key)}] {QuoteIfHasWhiteSpaces(o.Value)}"));
        }


        return builder.ToString();
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

public interface ICliOptionCapture
{
    string Name { get; }
}

public interface ICliMapOptionCapture : ICliOptionCapture
{
    string Key { get; }
}

public interface ICliCollectionCapture : ICliOptionCapture
{
    IEnumerable<string> Values { get; }
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
public sealed record CliScalarOptionCapture(string Name, string Value) : CliOptionCapture(Name), ICliCollectionCapture
{
    public IEnumerable<string> Values => FluentEnumerable.Yield(Value);
}

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
public sealed record CliKeyFlagOptionCapture(string Name, string Key) : CliOptionCapture(Name), ICliMapOptionCapture;

/// <summary>
/// Represents a captured command-line option with a key and a value (e.g., <c>--config[env] production</c>).
/// </summary>
/// <remarks>
/// The <see cref="CliKeyValueOptionCapture"/> record represents a command-line option that includes both a key and a value.
/// This is typically used for options where the key is paired with a value (e.g., <c>--config[env] production</c>).
/// The `Key` property holds the key part of the option, and the `Value` property holds the value associated with the key.
/// </remarks>
public sealed record CliKeyValueOptionCapture(string Name, string Key, string Value) : CliOptionCapture(Name), ICliMapOptionCapture;

/// <summary>
/// Represents a captured command-line option with a key and multiple associated values (e.g., <c>--config[env] test qa prod</c>).
/// </summary>
/// <remarks>
/// The <see cref="CliKeyCollectionOptionCapture"/> record represents a command-line option that includes both a key and a collection of values.
/// This is typically used for options where the key is paired with multiple values (e.g., <c>--config[env] test qa prod</c>).
/// The `Key` property holds the key part of the option (e.g., <c>env</c> in <c>--config[env]</c>), and the `Values` property holds the collection of values associated with the key (e.g., <c>test</c>, <c>qa</c>, and <c>prod</c>).
/// </remarks>
public sealed record CliKeyCollectionOptionCapture(string Name, string Key, ImmutableArray<string> Values) : CliOptionCapture(Name), ICliMapOptionCapture;
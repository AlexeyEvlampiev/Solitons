using System;
using System.Diagnostics;

namespace Solitons.CommandLine;

/// <summary>
/// Represents an exception that is thrown when there is a configuration error in the CLI.
/// This exception is typically thrown when an invalid or unsupported configuration is detected, 
/// such as an incorrect option type, missing configuration, or incompatible custom converter.
/// </summary>
/// <remarks>
/// This exception should be used for issues related to the CLI's internal configuration or setup that
/// must be corrected by the developer. For errors caused by invalid user input, the framework should throw a 
/// <see cref="CliExitException"/> instead.
/// </remarks>
public sealed class CliConfigurationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CliConfigurationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    [DebuggerNonUserCode]
    public CliConfigurationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CliConfigurationException"/> class with a specified error message 
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a <see langword="null"/> reference if no inner exception is specified.</param>
    [DebuggerNonUserCode]
    public CliConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    internal static CliConfigurationException UnsupportedOptionCollectionType(
        string option, 
        Type optionType)
    {
        return new CliConfigurationException(
            $"The option '{option}' specifies a collection type '{optionType.FullName}', " +
            "which cannot be instantiated dynamically due to its type constraints or configuration. " +
            "Ensure that the collection type has a parameterless constructor or is a supported collection type.");
    }

    public static CliConfigurationException OptionCollectionItemTypeMismatch(
        string option,
        Type customSampleType, 
        Type expectedItemType)
    {
        return new CliConfigurationException(
            $"The converted value of type '{customSampleType.FullName}' is not compatible with the expected collection " +
            $"item type '{expectedItemType.FullName}' " +
            $"for the '{option}' option. " +
            $"Verify that the custom converter produces values that match the expected type.");
    }


}


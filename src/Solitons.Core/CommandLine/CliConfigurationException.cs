using System;
using System.ComponentModel;
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
        Type converterType, 
        Type expectedItemType)
    {
        return new CliConfigurationException(
            $"The value converted by '{converterType.FullName}' is not compatible with the expected collection item type '{expectedItemType.FullName}' " +
            $"for the '{option}' option. Ensure that the custom converter produces values of the expected type.");
    }


    public static CliConfigurationException OptionValueConversionFailure(string option, string key, Type valueType)
    {
        return new CliConfigurationException(
            $"The option '{option}' is misconfigured. " +
            $"The input value for key '{key}' could not be converted to '{valueType}'. " +
            "Ensure that a valid type converter is provided.");
    }

    public static CliConfigurationException OptionSampleConversionWithCustomConverterFailed(
        string option, 
        string inputSample, 
        Type optionType,
        Type customConverterType)
    {
        return new CliConfigurationException(
            $"The sample value '{inputSample}' for the option '{option}' cannot be converted using the specified custom converter '{customConverterType.FullName}'. " +
            $"Ensure that the converter is compatible with the option type '{optionType.FullName}', " +
            $"or update the option type to align with the converter.");
    }

    public static CliConfigurationException InvalidOptionTypeConverter(string option, Type optionType, Type converterType)
    {
        return new CliConfigurationException(
            $"The option '{option}' of type '{optionType.FullName}' is invalid. " +
            $"The specified converter '{converterType.FullName}' is not able to convert the provided string to the required type. " +
            "Ensure that the converter is appropriate for the target option type or provide a valid custom converter.");
    }

    public static CliConfigurationException OptionDictionaryValueTypeMismatch(
        string option, 
        Type actualType, 
        Type expectedType)
    {
        return new CliConfigurationException(
            $"The converted value of type '{actualType.FullName}' is not compatible with the expected dictionary " +
            $"value type '{expectedType.FullName}' " +
            $"for the '{option}' option. " +
            $"Verify that the custom converter produces values that match the expected type.");
    }

    public static CliConfigurationException NotSupportedOptionDictionaryType(string option, Type optionType)
    {
        return new CliConfigurationException(
            $"The option '{option}' specifies an unsupported dictionary type '{optionType.FullName}'. " +
            "The dictionary key type must be 'string', and the dictionary must implement a supported interface. " +
            "Ensure the dictionary type is correctly configured.");

    }

    public static CliConfigurationException OptionTypeConversionFailure(string option, Type optionType)
    {
        return new CliConfigurationException(
            $"The '{optionType}' option value tokens cannot be converted from a string to the specified option type '{optionType}' using the default type converter. " +
            $"To resolve this, correct the option type if it's incorrect, or specify a custom type converter " +
            $"either by inheriting from '{typeof(CliOptionAttribute).FullName}' and overriding '{nameof(CliOptionAttribute.CanAccept)}()', " +
            $"or by applying the '{typeof(TypeConverterAttribute).FullName}' directly on the parameter or property.");
    }

    public static CliConfigurationException DefaultValueTypeMismatch(string option, Type optionType, object defaultValue)
    {
        return new CliConfigurationException(
            $"The provided default value is not of type {optionType}. Actual type is {defaultValue.GetType()}");
    }

    public static CliConfigurationException InvalidOptionInputConversion(string option, string input, Type valueType)
    {
        return new CliConfigurationException(
            $"The option '{option}' is not configured correctly. " +
            $"The input '{input}' could not be converted to the expected type '{valueType.FullName}'. " +
            "Ensure that the correct type converter is provided.");
    }

    public static CliConfigurationException InvalidCollectionOptionConversion(string option, Type itemType)
    {
        return new CliConfigurationException(
            $"The collection option '{option}' is not configured correctly. " +
            $"The input tokens could not be converted to the expected item type '{itemType.FullName}'. " +
            "Ensure that a valid type converter is available for the target type.");
    }

    public static CliConfigurationException CollectionElementParsingNotSupported()
    {
        throw new NotImplementedException();
    }
}


using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace Solitons.CommandLine.Reflection;

/// <summary>
/// Abstract base class for materializing CLI options from a command line.
/// </summary>
public abstract class CliOptionMaterializer
{
    /// <summary>
    /// Materializes an object from the given CLI command line.
    /// </summary>
    /// <param name="commandLine">The command line to materialize from.</param>
    /// <returns>The materialized object, or <c>null</c> if not applicable.</returns>
    public abstract object? Materialize(CliCommandLine commandLine);

    /// <summary>
    /// Creates a <see cref="CliOptionMaterializer"/> based on the provided attribute and declared type.
    /// </summary>
    /// <param name="attribute">The CLI option attribute.</param>
    /// <param name="declaredType">The declared type of the CLI option.</param>
    /// <param name="isOptional"></param>
    /// <param name="defaultValue"></param>
    /// <returns>A <see cref="CliOptionMaterializer"/> instance.</returns>
    /// <exception cref="CliConfigurationException">Thrown if a materializer cannot be created.</exception>
    public static CliOptionMaterializer CreateOrThrow(
        CliOptionAttribute attribute,
        Type declaredType,
        bool isOptional,
        object? defaultValue)
    {
        var materializer =
            CliDictionaryOptionMaterializer.TryCreateMaterializer(attribute, declaredType) ??
            CliFlagOptionMaterializer.TryCreateMaterializer(attribute, declaredType, isOptional) ??
            CliScalarOptionMaterializer.TryCreateMaterializer(attribute, declaredType, isOptional, defaultValue);
        if (materializer is null)
        {
            throw new CliConfigurationException(
                $"Unable to create materializer for type '{declaredType.FullName}' with the attribute '{attribute}'. Ensure the type is supported.");

        }

        return materializer;
    }
}

/// <summary>
/// Represents an exception thrown when materializing a CLI option fails.
/// </summary>
sealed  class CliOptionMaterializationException : FormatException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CliOptionMaterializationException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public CliOptionMaterializationException(string message) : base(message)
    {
        
    }
}

/// <summary>
/// Materializes key-value CLI options into a dictionary.
/// </summary>
internal sealed class CliDictionaryOptionMaterializer : CliOptionMaterializer
{
    private readonly CliOptionAttribute _attribute;
    private readonly Func<IDictionary> _factory;
    private readonly Func<string, object> _valueParser;

    private CliDictionaryOptionMaterializer(
        CliOptionAttribute attribute,
        Func<IDictionary> factory,
        Func<string, object> valueParser)
    {
        _attribute = attribute;
        _factory = factory;
        _valueParser = valueParser;
    }


    /// <summary>
    /// Attempts to create a <see cref="CliDictionaryOptionMaterializer"/> if the declared type is supported.
    /// </summary>
    /// <param name="attribute">The CLI option attribute.</param>
    /// <param name="declaredType">The declared type.</param>
    /// <returns>A <see cref="CliDictionaryOptionMaterializer"/>, or <c>null</c> if not applicable.</returns>
    internal static CliDictionaryOptionMaterializer? TryCreateMaterializer(
        CliOptionAttribute attribute,
        Type declaredType)
    {
        return declaredType
            .GetGenericDictionaryArgumentTypes()
            .SelectMany(pair =>
            {
                // pair: KeyValuePair<Type, Type>
                if (pair.Key == typeof(string) &&
                    attribute.CanAccept(pair.Value, out TypeConverter valueConverter))
                {
                    var comparer = attribute.GetValueComparer();
                    var targetType = typeof(Dictionary<,>).MakeGenericType([typeof(string), pair.Value]);
                    var ctor = targetType.GetConstructor([typeof(IEqualityComparer<string>)])!;
                    IDictionary CreateDictionary() => (IDictionary)ctor.Invoke([comparer]);

                    object ParseValue(string value)
                    {
                        var result = valueConverter.ConvertFromInvariantString(value);
                        if (result is null)
                        {
                            throw new CliOptionMaterializationException($"Failed to parse value: '{value}'.");
                        }
                        return result;
                    }
                    return [new CliDictionaryOptionMaterializer(attribute, CreateDictionary, ParseValue)];
                }

                return Enumerable.Empty<CliDictionaryOptionMaterializer>();
            })
            .SingleOrDefault();
    }

    /// <inheritdoc/>
    public override object? Materialize(CliCommandLine commandLine)
    {
        var options = commandLine.Options
            .Where(option => _attribute.IsMatch(option.Name))
            .ToList();

        var result = _factory.Invoke();
        foreach (var option in options)
        {
            switch (option)
            {
                case CliKeyValueOptionCapture capture:
                    var value = _valueParser.Invoke(capture.Value);
                    result.Add(capture.Key, value);
                    break;

                case CliKeyCollectionOptionCapture:
                    throw new CliExitException(
                        $"{option.Name} option expected key-value pair but received a collection.");

                case CliKeyFlagOptionCapture:
                    throw new CliExitException(
                        $"{option.Name} option expected key-value pair, but the value is missing.");

                default:
                    throw new CliExitException(
                        $"{option.Name} option expected key-value pair. Example: {option.Name}.key value.");
            }
        }

        return result;
    }
}

internal sealed class CliScalarOptionMaterializer : CliOptionMaterializer
{
    private readonly CliOptionAttribute _attribute;
    private readonly Func<string, object> _valueParser;
    private readonly bool _isOptional;
    private readonly object? _defaultValue;

    private CliScalarOptionMaterializer(
        CliOptionAttribute attribute,
        Func<string, object> valueParser,
        bool isOptional,
        object? defaultValue)
    {
        _attribute = attribute;
        _valueParser = valueParser;
        _isOptional = isOptional;
        _defaultValue = defaultValue;
    }
    public override object? Materialize(CliCommandLine commandLine)
    {
        var options = commandLine.Options
            .Where(option => _attribute.IsMatch(option.Name))
            .ToList();
        if (options.Count > 1)
        {
            throw new CliOptionMaterializationException(
                $"The option '{_attribute.PipeSeparatedAliases}' was specified multiple times. Ensure only one instance of this option is provided.");
        }

        if (options.Count == 0)
        {
            if (_isOptional)
            {
                return _defaultValue;
            }

            throw new CliOptionMaterializationException(
                $"The required option '{_attribute.PipeSeparatedAliases}' is missing. Please provide this option and try again.");
        }

        var option = options.Single();
        return option switch
        {
            CliScalarOptionCapture capture => _valueParser.Invoke(capture.Value),
            ICliMapOptionCapture => throw new CliOptionMaterializationException(
                $"The option '{option.Name}' cannot accept a keyed map. Provide a single value instead."),
            CliCollectionOptionCapture => throw new CliOptionMaterializationException(
                $"The option '{option.Name}' cannot accept a collection of values. Provide a single value instead."),
            CliFlagOptionCapture => throw new CliOptionMaterializationException(
                $"The option '{option.Name}' cannot be used as a flag. Provide a single value instead."),
            _ => throw new CliOptionMaterializationException(
                $"The option '{option.Name}' is not in the expected format. Refer to the documentation for valid usage.")
        };
    }

    public static CliOptionMaterializer TryCreateMaterializer(
        CliOptionAttribute attribute, 
        Type declaredType,
        bool isOptional,
        object? defaultValue)
    {
        Debug.WriteLine(attribute.PipeSeparatedAliases);
        if (false == attribute.CanAccept(declaredType, out var typeConverter))
        {
            throw new CliConfigurationException(
                $"The attribute of type '{attribute.GetType().Name}' cannot accept the declared type '{declaredType.FullName}'. " +
                $"Refer to the attribute's aliases: '{attribute.PipeSeparatedAliases}' to identify the problematic configuration. " +
                "Ensure the declared type is compatible with the attribute's requirements.");
        }


        object ParseValue(string value)
        {
            try
            {
                var result = typeConverter.ConvertFromInvariantString(value);
                if (result == null)
                {
                    throw new CliOptionMaterializationException(
                        $"The value '{value}' cannot be converted to the required type '{declaredType.FullName}'.");
                }
                return result;
            }
            catch (FormatException ex) when(declaredType.IsEnum)
            {
                var expectedValueCsv = Enum
                    .GetNames(declaredType)
                    .Join(", ");
                throw new CliOptionMaterializationException(
                    $"The value '{value}' is invalid. Expected: {expectedValueCsv}. {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new CliOptionMaterializationException(
                    $"The value '{value}' is invalid. {ex.Message}");
            }
        }

        return new CliScalarOptionMaterializer(attribute, ParseValue, isOptional, defaultValue);
    }
}

internal sealed class CliFlagOptionMaterializer : CliOptionMaterializer
{
    private readonly CliOptionAttribute _attribute;
    private readonly bool _isRequired;
    private readonly object _defaultValue;

    private CliFlagOptionMaterializer(
        CliOptionAttribute attribute,
        bool isOptional,
        object defaultValue)
    {
        _attribute = attribute;
        _isRequired = !(isOptional);
        _defaultValue = defaultValue;
    }
    public override object? Materialize(CliCommandLine commandLine)
    {
        var flag = commandLine.Options
            .Where(option => _attribute.IsMatch(option.Name))
            .Select(option => option switch {
                CliFlagOptionCapture => _defaultValue,
                ICliMapOptionCapture => throw new CliOptionMaterializationException(
                    $"The flag option '{option.Name}' cannot be used as a map."),
                _ => throw new CliOptionMaterializationException(
                    $"The flag option '{option.Name}' cannot accept values.")
            })
            .FirstOrDefault();
        if (flag is null && _isRequired)
        {
            throw new CliOptionMaterializationException(
                $"The required flag option '{_attribute.PipeSeparatedAliases}' is missing.");
        }
        return flag;
    }

    public static CliOptionMaterializer? TryCreateMaterializer(
        CliOptionAttribute attribute, 
        Type declaredType,
        bool isOptional)
    {
        if (CliFlag.IsFlagType(declaredType, out var defaultValue))
        {
            return new CliFlagOptionMaterializer(attribute, isOptional, defaultValue);
        }

        return null;
    }
}
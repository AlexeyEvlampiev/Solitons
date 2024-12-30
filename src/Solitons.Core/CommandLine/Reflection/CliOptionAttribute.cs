using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

namespace Solitons.CommandLine.Reflection;

/// <summary>
/// Attribute to define command line options for a method or property.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public class CliOptionAttribute : Attribute
{
    private static readonly Regex SpecificationRegex;
    private static readonly Regex OptionRegex;
    private readonly Regex _optionRegex;

    static CliOptionAttribute()
    {
        SpecificationRegex = new Regex(
            @"^$option(\|$option)*$"
            .Replace("$option", "(?<option>--?[^|]+)"));
        OptionRegex = new Regex(@"(?<=^[-]{1,2})[\w\?]+(?:[-]+[\w\?]+)*$");
    }

    /// <summary>
    /// Constructs a CLI option attribute with specified options and an optional description.
    /// </summary>
    /// <param name="specification">A pipe-separated string containing different CLI options.</param>
    /// <param name="description">A description of what the options do.</param>
    /// <exception cref="ArgumentException">Thrown when the options format is invalid or contains duplicates.</exception>
    public CliOptionAttribute(string specification, string description = "")
    {
        // Regex to split the pattern and validate format
        var namePatternMatch = ThrowIf
            .ArgumentNullOrWhiteSpace(specification)
            .Convert(SpecificationRegex.Match);

        if (!namePatternMatch.Success)
        {
            throw new ArgumentException("Invalid pattern format. Expected: --option1|--option2|-o|...",
                nameof(specification));
        }

        var names = new HashSet<string>(StringComparer.Ordinal);
        var longNames = new List<string>();
        var shortNames = new List<string>();

        foreach (Capture capture in namePatternMatch.Groups["option"].Captures)
        {
            var match = OptionRegex.Match(capture.Value);
            var value = match.Value;
            if (false == match.Success)
            {
                throw new ArgumentException($"Invalid parameter name option: {capture}",
                    nameof(specification));
            }

            if (!names.Add(capture.Value))
            {
                throw new ArgumentException($"Duplicate option name detected: '{value}'", nameof(specification));
            }

            if (capture.Value.StartsWith("--"))
            {
                longNames.Add(value);
            }
            else
            {
                shortNames.Add(value);
            }

        }

        if (longNames.Count == 0 && shortNames.Count == 0)
        {
            throw new ArgumentException("At least one long or short name must be specified.", nameof(specification));
        }


        Description = description;
        Aliases = names.ToArray().AsReadOnly();
        LongOptionNames = longNames.AsReadOnly();
        ShortOptionNames = shortNames.AsReadOnly();
        Description = description;

        PipeSeparatedAliases = LongOptionNames
            .OrderByDescending(n => n.Length)
            .Select(n => $"--{n}")
            .Union(ShortOptionNames
                .OrderByDescending(n => n.Length)
                .Select(n => $"-{n}"))
            .Join("|");

        OptionAliasesCsv = ShortOptionNames
            .Select(o => $"-{o}")
            .Union(LongOptionNames.Select(o => $"--{o}"))
            .Join(", ");

        _optionRegex = new Regex($"(?xis-m)^(?:{PipeSeparatedAliases})$");
    }

    /// <summary>
    /// Comma-separated list of all options.
    /// </summary>
    public string OptionAliasesCsv { get; }

    /// <summary>
    /// Specification of options as used in the CLI.
    /// </summary>
    public string PipeSeparatedAliases { get; }

    /// <summary>
    /// Description of the CLI options.
    /// </summary>
    public string Description { get; }

    public IReadOnlyList<string> Aliases { get; }

    /// <summary>
    /// List of short option names.
    /// </summary>
    public IReadOnlyList<string> ShortOptionNames { get; }

    /// <summary>
    /// List of long option names.
    /// </summary>
    public IReadOnlyList<string> LongOptionNames { get; }

    public virtual bool AllowsCsv => true;

    public string? DefaultValue { get; init; }


    /// <summary>
    /// Returns a string representation of the option specification.
    /// </summary>
    /// <returns>A string that represents the option specification.</returns>
    public override string ToString() => PipeSeparatedAliases;

    public virtual bool CanAccept(Type optionType, out TypeConverter converter)
    {
        converter = TypeDescriptor.GetConverter(optionType);

        if (optionType == typeof(TimeSpan))
        {
            converter = new MultiFormatTimeSpanConverter();
            ThrowIf.False(converter.CanConvertFrom(typeof(string)));
        }
        else if (optionType == typeof(CancellationToken))
        {
            converter = new CliCancellationTokenTypeConverter();
            ThrowIf.False(converter.CanConvertFrom(typeof(string)));
        }

        if (converter.SupportsCliOperandConversion())
        {
            return true;
        }

        var valueConverter = optionType
            .GetGenericDictionaryArgumentTypes()
            .Where(args => args.Key == typeof(string))
            .Select(args => args.Value)
            .Union(optionType.GetGenericEnumerableArgumentTypes())
            .SelectMany(type =>
            {
                if (CanAccept(type, out var valueConverter))
                {
                    return [valueConverter];
                }

                return Enumerable.Empty<TypeConverter>();
            })
            .FirstOrDefault();

        if (valueConverter is not null)
        {
            converter = valueConverter;
            return true;
        }

        return false;
    }

    public virtual StringComparer GetValueComparer() => StringComparer.OrdinalIgnoreCase;



    public static CliOptionAttribute Get(PropertyInfo property, Attribute[] attributes)
    {
        var option = attributes.OfType<CliOptionAttribute>().Single();
        var descriptionOverride = attributes
            .OfType<DescriptionAttribute>()
            .Select(a => a.Description)
            .SingleOrDefault(option.Description);
        return new CliOptionAttribute(option.PipeSeparatedAliases, descriptionOverride);
    }

    [DebuggerNonUserCode]
    public bool IsMatch(string optionName) => _optionRegex.IsMatch(optionName);

    public bool IsOptional(ParameterInfo parameter, out object? defaultValue)
    {
        defaultValue = null;
        var result =
            parameter.HasDefaultValue ||
            parameter.IsNullable() ||
            this.DefaultValue is not null;

        if (!result)
        {
            defaultValue = null;
            return false;
        }

        if (parameter.DefaultValue is not null && 
            parameter.DefaultValue is not DBNull)
        {
            defaultValue = parameter.DefaultValue;
            return true;
        }

        if (this.DefaultValue is null)
        {
            defaultValue = null;
            return true;
        }

        if (CanAccept(parameter.ParameterType, out var valueConverter))
        {
            try
            {
                defaultValue = valueConverter.ConvertFromInvariantString(this.DefaultValue);
                return true;
            }
            catch (Exception e)
            {
                throw new CliConfigurationException(
                    $"The default value '{DefaultValue}' provided cannot be used to initialize the parameter '{parameter.Name}' of type '{parameter.ParameterType.FullName}'. " +
                    $"Ensure the default value matches the expected type and format for '{parameter.ParameterType.FullName}'. Additional details: {e.Message}");
            }
        }

        throw new CliConfigurationException(
            $"The parameter '{parameter.Name}' of type '{parameter.ParameterType.FullName}' is incompatible with the applied CLI attribute. " +
            $"Verify that the parameter type supports conversion from CLI options and that any default values are properly defined. " +
            $"If this issue persists, consider reviewing the CLI attribute configuration or consulting the documentation for guidance.");
    }

    public bool IsOptional(PropertyInfo property, out object? defaultValue)
    {
        defaultValue = null;

        // Determine if the property is nullable or has a defined default value
        var result =
            property.IsNullable() ||
            this.DefaultValue is not null;

        if (!result)
        {
            defaultValue = null;
            return false;
        }

        // If the attribute specifies a default value
        if (this.DefaultValue is not null)
        {
            if (CanAccept(property.PropertyType, out var valueConverter))
            {
                try
                {
                    defaultValue = valueConverter.ConvertFromInvariantString(this.DefaultValue);
                    return true;
                }
                catch (Exception e)
                {
                    throw new CliConfigurationException(
                        $"The default value '{DefaultValue}' cannot be assigned to the property '{property.Name}' of type '{property.PropertyType.FullName}'. " +
                        $"Ensure the default value matches the expected type and format. Additional details: {e.Message}");
                }
            }

            throw new CliConfigurationException(
                $"The property '{property.Name}' of type '{property.PropertyType.FullName}' does not support conversion from CLI options. " +
                $"Verify that the property type is compatible with CLI attributes or modify the type to support such conversion.");
        }

        // If nullable, no specific default value is required
        if (property.IsNullable())
        {
            defaultValue = null;
            return true;
        }

        return false;
    }

}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

public abstract class CliOptionBundle
{
    public static bool IsAssignableFrom(Type type) => typeof(CliOptionBundle).IsAssignableFrom(type);

    // [DebuggerStepThrough]
    internal static IEnumerable<JazzyOptionInfo> GetOptions(Type type)
    {
        if (false == IsAssignableFrom(type))
        {
            throw new ArgumentException();
        }

        CliOptionBundle bundle;
        try
        {
            bundle = ThrowIf.NullReference(Activator.CreateInstance(type) as CliOptionBundle);
        }
        catch (Exception e)
        {
            throw new CliConfigurationException(
                $"An error occurred while attempting to instantiate the options bundle of type '{type}'. " +
                $"Ensure the type has a parameterless constructor and is a valid CliOptionBundle. {e.Message}");
        }

        return bundle.GetOptions();
    }

    internal IEnumerable<JazzyOptionInfo> GetOptions()
    {
        var type = GetType();
        CliOptionBundle bundle;
        try
        {
            bundle = ThrowIf.NullReference(Activator.CreateInstance(type) as CliOptionBundle);
        }
        catch (Exception e)
        {
            throw new CliConfigurationException(
                $"An error occurred while attempting to instantiate the options bundle of type '{type}'. " +
                $"Ensure the type has a parameterless constructor and is a valid CliOptionBundle. {e.Message}");
        }

        var properties = type.GetProperties();
        foreach (var property in properties)
        {
            if (IsAssignableFrom(property.PropertyType))
            {
                throw new CliConfigurationException($"The property '{property.Name}' in the bundle of type '{type}' contains a nested bundle property, which is not allowed. " +
                                                    $"Please ensure that bundle properties do not contain other bundles.");

            }

            var attributes = property.GetCustomAttributes(true).ToList();
            var optionAtt = attributes.OfType<CliOptionAttribute>().SingleOrDefault();
            if (optionAtt is null)
            {
                Debug.WriteLine("Not an cli options");
                continue;
            }

            var description = attributes
                .OfType<DescriptionAttribute>()
                .Select(attribute => attribute.Description)
                .Union(attributes.OfType<CliOptionAttribute>().Select(attribute => attribute.Description))
                .Union([$"'{type.Name}' options bundle property."])
                .First();
            var defaultValue = property.GetValue(bundle);
            yield return new JazzyOptionInfo(optionAtt, defaultValue, description, property.PropertyType)
            {
                IsRequired = attributes.OfType<RequiredAttribute>().Any()
            };
        }
    }

    public void PopulateOptions(Match match, CliTokenDecoder decoder)
    {
        throw new NotImplementedException();
    }

    public static object Create(Type bundleType, Match commandLineMatch)
    {
        throw new NotImplementedException();
    }

    [DebuggerStepThrough]
    internal IEnumerable<ICliCommandOptionFactory> BuildOptionFactories()
    {
        return BuildOptionFactories(GetType());
    }

    [DebuggerStepThrough]
    internal static IEnumerable<ICliCommandOptionFactory> BuildOptionFactories<T>() where T : CliOptionBundle, new() => BuildOptionFactories(typeof(T));

    internal static IEnumerable<ICliCommandOptionFactory> BuildOptionFactories(Type bundleType)
    {
        return bundleType
            .GetProperties()
            .SelectMany(property =>
            {
                if (IsAssignableFrom(property.PropertyType))
                {
                    throw new InvalidOperationException("Nested bundles are not allowed.");
                }

                var option = property
                    .GetCustomAttributes(true)
                    .OfType<CliOptionAttribute>()
                    .SingleOrDefault();
                if (option is not null)
                {
                    return [new CliCommandPropertyOptionFactory(property, option)];
                }

                return Enumerable.Empty<ICliCommandOptionFactory>();
            });
    }

    internal static CliDeserializer CreateDeserializerFor(Type parameterParameterType)
    {
        throw new NotImplementedException();
    }
}
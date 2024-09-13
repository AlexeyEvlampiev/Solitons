using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Solitons.Caching;

namespace Solitons.CommandLine;

public abstract class CliOptionBundle
{
    private static readonly ConcurrentDictionary<Type, Dictionary<CliOptionInfo, PropertyInfo>> 
        OptionsByBundleType = new();

    public static bool IsAssignableFrom(Type type) => typeof(CliOptionBundle).IsAssignableFrom(type);

    // [DebuggerStepThrough]
    internal static Dictionary<CliOptionInfo, PropertyInfo> GetOptions(Type type, IInMemoryCache cache)
    {
        return OptionsByBundleType.GetOrAdd(type, () => LoadOptions(type, cache));
    }

    internal Dictionary<CliOptionInfo, PropertyInfo> GetOptions(IInMemoryCache cache)
    {
        return OptionsByBundleType.GetOrAdd(GetType(), () => LoadOptions(GetType(), cache));
    }

    private static Dictionary<CliOptionInfo, PropertyInfo> LoadOptions(Type bundleType, IInMemoryCache cache)
    {
        if (false == IsAssignableFrom(bundleType))
        {
            throw new ArgumentException();
        }

        CliOptionBundle bundle;
        try
        {
            bundle = ThrowIf.NullReference(Activator.CreateInstance(bundleType) as CliOptionBundle);
        }
        catch (Exception e)
        {
            throw new CliConfigurationException(
                $"An error occurred while attempting to instantiate the options bundle of type '{bundleType}'. " +
                $"Ensure the type has a parameterless constructor and is a valid CliOptionBundle. {e.Message}");
        }

        var result = new Dictionary<CliOptionInfo, PropertyInfo>();

        var properties = bundleType.GetProperties();
        foreach (var property in properties)
        {
            if (IsAssignableFrom(property.PropertyType))
            {
                throw new CliConfigurationException($"The property '{property.Name}' in the bundle of type '{bundleType}' contains a nested bundle property, which is not allowed. " +
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
                .Union([$"'{bundleType.Name}' options bundle property."])
                .First();
            var defaultValue = property.GetValue(bundle);
            var info = new CliOptionInfo(optionAtt, property.Name, defaultValue, description, property.PropertyType)
            {
                IsRequired = attributes.OfType<RequiredAttribute>().Any()
            };
            result.Add(info, property);
        }

        return result;
    }

    public void PopulateOptions(Match commandLineMatch, CliTokenDecoder decoder, IInMemoryCache cache)
    {
        var options = GetOptions(cache);
        foreach (var pair in options)
        {
            var (option, property) = (pair.Key, pair.Value);
            var value = option.Deserialize(commandLineMatch, decoder);
            property.SetValue(this, value);
        }
    }

    public static object Create(Type bundleType, Match commandLineMatch)
    {
        throw new NotImplementedException();
    }



    internal static CliDeserializer CreateDeserializerFor(
        Type bundleType, 
        IInMemoryCache cache,
        out IEnumerable<CliOptionInfo> options)
    {
        ThrowIf.False(IsAssignableFrom(bundleType));
        var ctor = bundleType.GetConstructor([]);
        if (ctor is null)
        {
            throw new CliConfigurationException("Oops...");
        }

        var map = GetOptions(bundleType, cache);

        options = map.Keys;
        return Deserialize;
        object Deserialize(Match commandLineMatch, CliTokenDecoder decoder)
        {
            var bundle = ctor.Invoke([]);
            foreach (var option in map)
            {
                var (info, property) = (option.Key, option.Value);
                var value = info.Deserialize(commandLineMatch, decoder);
                property.SetValue(bundle, value);
            }

            return bundle;
        }
    }
}
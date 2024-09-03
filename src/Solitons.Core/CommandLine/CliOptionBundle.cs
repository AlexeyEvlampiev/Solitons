using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

public abstract class CliOptionBundle
{
    private static readonly ConcurrentDictionary<Type, CliBundleOptionInfo[]> ParametersByType = new();

    public static bool IsAssignableFrom(Type type) => typeof(CliOptionBundle).IsAssignableFrom(type);

   // [DebuggerStepThrough]
    internal static IEnumerable<CliBundleOptionInfo> GetOptions(Type type)
    {
        if (false == IsAssignableFrom(type))
        {
            throw new ArgumentException();
        }
        
        return ParametersByType
            .GetOrAdd(type, () => type
                .GetProperties()
                .Select(pi => new CliBundleOptionInfo(pi))
                .ToArray())
            .AsEnumerable();
    }

    public void PopulateFrom(Match match, CliTokenSubstitutionPreprocessor preprocessor)
    {
        foreach (var parameter in GetOptions(GetType()))
        {
            parameter.SetValues(this, match, preprocessor);
        }
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
}
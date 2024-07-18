using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

public abstract class CliOptionBundle
{
    private static readonly ConcurrentDictionary<Type, CliBundleOption[]> ParametersByType = new();

    public static bool IsAssignableFrom(Type type) => typeof(CliOptionBundle).IsAssignableFrom(type);

   // [DebuggerStepThrough]
    internal static IEnumerable<CliBundleOption> GetOptions(Type type)
    {
        if (false == IsAssignableFrom(type))
        {
            throw new ArgumentException();
        }
        
        return ParametersByType
            .GetOrAdd(type, () => type
                .GetProperties()
                .Select(pi => new CliBundleOption(pi))
                .ToArray())
            .AsEnumerable();
    }

    public void Populate(Match match)
    {
        foreach (var parameter in GetOptions(GetType()))
        {
            parameter.SetValues(this, match);
        }
    }
}
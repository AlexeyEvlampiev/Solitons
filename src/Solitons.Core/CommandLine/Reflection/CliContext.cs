using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Solitons.CommandLine.Reflection;

public sealed record CliContext
{
    private readonly ImmutableArray<CliGlobalOptionBundle> _globalOptionBundles;
    public static readonly CliContext Empty = new ([], []);


    public CliContext(
        IEnumerable<CliRouteAttribute> rootRoutes,
        IEnumerable<CliGlobalOptionBundle> globalOptionBundles)
    {
        RootRoutes = rootRoutes;
        _globalOptionBundles = [..globalOptionBundles];
        GlobalOptions = [.. _globalOptionBundles.SelectMany(bundle => bundle.GetOptions()).Distinct()];
    }


    public IEnumerable<CliRouteAttribute> RootRoutes { get; }

    internal CliGlobalOptionBundle[] ToGlobalOptionBundles(CliCommandLine commandLine)
    {
        var bundles = new List<CliGlobalOptionBundle>(_globalOptionBundles.Length);
        foreach (var bundle in _globalOptionBundles)
        {
            var clone = bundle.Clone();
            clone.PopulateFrom(commandLine);
            bundles.Add(clone);
        }
        return bundles.ToArray();
    }

    public ImmutableArray<string> RootRouteSegments { get; }
    public ImmutableArray<ICliOptionMemberInfo> GlobalOptions { get; }
}
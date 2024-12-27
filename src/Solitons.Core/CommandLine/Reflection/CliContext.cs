using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Solitons.CommandLine.Reflection;

public sealed record CliContext
{
    public static readonly CliContext Empty = new ([], Enumerable.Empty<ICliOptionMemberInfo>());


    public CliContext(
        IEnumerable<CliRouteAttribute> rootRoutes,
        IEnumerable<CliGlobalOptionBundle> globalOptions) 
        : this(rootRoutes, globalOptions.SelectMany(bundle => bundle.GetOptions()))
    {
        
    }

    public CliContext(
        IEnumerable<CliRouteAttribute> rootRoutes,
        IEnumerable<ICliOptionMemberInfo> globalOptions)
    {
        RootRouteSegments = [.. rootRoutes.SelectMany(route => route.Segments)];
        GlobalOptions = [.. globalOptions.Distinct()];
    }



    public ImmutableArray<string> RootRouteSegments { get; }
    public ImmutableArray<ICliOptionMemberInfo> GlobalOptions { get; }
}
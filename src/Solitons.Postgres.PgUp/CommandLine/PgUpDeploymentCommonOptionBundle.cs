using Solitons.CommandLine;
using Solitons.CommandLine.Reflection;

namespace Solitons.Postgres.PgUp.CommandLine;

public sealed class PgUpDeploymentCommonOptionBundle : CliOptionBundle
{
    [PgUpParametersOption]
    public Dictionary<string, string> Parameters { get; set; } = new();

    [CliOption("--timeout")]
    public TimeSpan? Timeout { get; set; }



    sealed class PgUpParametersOptionAttribute()
        : CliOptionAttribute("--parameters|--parameter|-p", "Defines parameters for customizing deployment scripts.")
    {
        public override StringComparer GetValueComparer() => StringComparer.OrdinalIgnoreCase;
    }
}
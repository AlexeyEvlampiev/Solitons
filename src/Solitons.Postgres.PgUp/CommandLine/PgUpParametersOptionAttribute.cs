using Solitons.CommandLine.Reflection;

namespace Solitons.Postgres.PgUp.CommandLine;

sealed class PgUpParametersOptionAttribute()
    : CliOptionAttribute("--parameters|--parameter|-p", "Defines parameters for customizing deployment scripts.")
{
    public override StringComparer GetValueComparer() => StringComparer.OrdinalIgnoreCase;
}
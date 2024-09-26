using System.Collections.Immutable;

namespace Solitons.CommandLine.Models;

internal sealed record CliModel
{
    public required string Logo { get; init; }
    public required string Description { get; init; }
    public required ImmutableArray<CliCommandModel> Commands { get; init; }
}

internal sealed record CliCommandModel
{
    public required string Synopsis { get; init; }

    public required string Description { get; init; }

    public required ImmutableArray<CliCommandSegmentModel> CommandSegments { get; init; }
}

internal  record CliCommandSegmentModel
{
    public required string RegularExpression { get; init; }
}

internal sealed record CliCommandArgumentModel : CliCommandSegmentModel
{
    public required string Name { get; init; }

    public required string RegexGroupName { get; init; }

    public required string Description { get; init; }
}
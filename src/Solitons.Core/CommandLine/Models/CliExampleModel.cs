namespace Solitons.CommandLine.Models;

internal sealed record CliExampleModel
{
    public required string Example { get; init; }
    public required string Description { get; init; }
    public override string ToString() => Example;
}
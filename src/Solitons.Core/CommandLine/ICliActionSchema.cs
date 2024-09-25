using System.Collections.Generic;

namespace Solitons.CommandLine;

internal interface ICliActionSchema
{
    int CommandSegmentsCount { get; }

    int ExamplesCount { get; }

    string Description { get; }

    string GetSegmentRegularExpression(int segmentIndex);

    string GetOptionRegularExpression(int optionIndex);

    string GetSynopsis();
    bool IsArgumentSegment(int segmentIndex);

    IEnumerable<Argument> Arguments { get; }

    IEnumerable<Option> Options { get; }

    IEnumerable<Example> Examples { get; }
    int OptionsCount { get; }

    Argument GetArgument(int argumentIndex);

    Example GetExample(int exampleIndex);


    public sealed record Argument(string Name, string Description);
    public sealed record Option(string Name, string Description);
    public sealed record Example(string Command, string Description);
}
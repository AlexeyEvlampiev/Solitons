using System.Collections.Generic;
using System.Diagnostics;

namespace Solitons.CommandLine;

internal interface ICliRouteCommandSegmentMetadata : ICliRouteSegmentMetadata
{
    string PipeAliasesPattern { get; }

    [DebuggerStepThrough]
    string ICliRouteSegmentMetadata.BuildRegularExpression(
        IReadOnlyList<ICliRouteSegmentMetadata> segments) =>
        BuildRegularExpression();

    public sealed string BuildRegularExpression() => $"(?:{PipeAliasesPattern})";
}
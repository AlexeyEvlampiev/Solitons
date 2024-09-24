using System.Collections.Generic;

namespace Solitons.CommandLine;

interface ICliRouteSegmentMetadata
{
    public bool IsArgument => (this is ICliRouteArgumentSegmentMetadata);

    string BuildRegularExpression(
        IReadOnlyList<ICliRouteSegmentMetadata> segments);
}
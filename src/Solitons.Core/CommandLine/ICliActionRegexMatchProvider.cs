using System.Collections.Generic;

namespace Solitons.CommandLine;

internal interface ICliActionRegexMatchProvider
{
    IEnumerable<object> GetCommandSegments();
}
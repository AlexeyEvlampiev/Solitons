using System.Collections.Generic;

namespace Solitons.CommandLine;

internal interface ICliSubCommandInfo
{
    IEnumerable<string> GetAliases();
}
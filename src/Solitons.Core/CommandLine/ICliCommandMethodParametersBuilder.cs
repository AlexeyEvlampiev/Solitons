using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal interface ICliCommandMethodParametersFactory
{
    object?[] BuildMethodArguments(Match match, CliTokenDecoder decoder);
    IEnumerable<ICliCommandOptionFactory> OptionFactories { get; }
}
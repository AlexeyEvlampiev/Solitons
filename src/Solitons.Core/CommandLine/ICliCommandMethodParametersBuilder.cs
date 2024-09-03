using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal interface ICliCommandMethodParametersFactory
{
    object?[] BuildMethodArguments(Match match, ICliTokenSubstitutionPreprocessor preProcessor);
    IEnumerable<ICliCommandOptionFactory> OptionFactories { get; }
}
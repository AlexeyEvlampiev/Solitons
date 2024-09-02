using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal interface ICliCommandMethodParametersBuilder
{
    object?[] BuildMethodArguments(Match match, ICliTokenSubstitutionPreprocessor preProcessor);
    IEnumerable<ICliCommandOptionBuilder> GetAllCommandOptions();
}
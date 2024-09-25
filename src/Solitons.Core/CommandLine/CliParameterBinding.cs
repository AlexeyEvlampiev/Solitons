using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

delegate object? CliOperandMaterializer(Match commandLineMatch, CliTokenDecoder decoder);
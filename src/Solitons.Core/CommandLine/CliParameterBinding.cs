using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

delegate object? CliDeserializer(Match commandLineMatch, CliTokenDecoder decoder);
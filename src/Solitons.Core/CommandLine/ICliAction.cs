using System.Collections.Generic;
using Solitons.Caching;

namespace Solitons.CommandLine;

internal interface ICliAction
{
    bool IsMatch(string commandLine);
    double Rank(string commandLine);
    int Execute(string commandLine, CliTokenDecoder decoder, IMemoryCache cache);

}
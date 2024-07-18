using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine.ZapCli;

internal sealed class ZapCliActionSimilarity : CommandLineInterface.Similarity
{
    private readonly int _matchedGroupsCount;

    public ZapCliActionSimilarity(Match match)
    {
        if (match.Success == false)
        {
            throw new ArgumentException();
        }
        
        _matchedGroupsCount = match.Groups
            .OfType<Group>()
            .Count(g => g.Success);
    }

    protected override int CompareTo(CommandLineInterface.Similarity other)
    {
        var x = (ZapCliActionSimilarity)other;
        return x._matchedGroupsCount - this._matchedGroupsCount;
    }
}
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal sealed class CliActionSimilarity : CliProcessor.Similarity
{
    private readonly int _matchedGroupsCount;

    public CliActionSimilarity(Match match)
    {
        if (match.Success == false)
        {
            throw new ArgumentException();
        }

        _matchedGroupsCount = match.Groups
            .OfType<Group>()
            .Count(g => g.Success);
    }

    protected override int CompareTo(CliProcessor.Similarity other)
    {
        var x = (CliActionSimilarity)other;
        return  _matchedGroupsCount - x._matchedGroupsCount;
    }
}
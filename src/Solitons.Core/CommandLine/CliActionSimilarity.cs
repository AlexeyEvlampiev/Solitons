using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal sealed class CliActionSimilarity : IComparable<CliActionSimilarity>
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

    public int CompareTo(CliActionSimilarity other)
    {
        return  _matchedGroupsCount - other._matchedGroupsCount;
    }

}
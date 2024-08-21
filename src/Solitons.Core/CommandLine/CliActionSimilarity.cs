using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal sealed class CliActionSimilarity : IComparable<CliActionSimilarity>, IEquatable<CliActionSimilarity>
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



    public override string ToString() => _matchedGroupsCount.ToString();

    public bool Equals(CliActionSimilarity? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return _matchedGroupsCount == other._matchedGroupsCount;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is CliActionSimilarity other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _matchedGroupsCount;
    }
}
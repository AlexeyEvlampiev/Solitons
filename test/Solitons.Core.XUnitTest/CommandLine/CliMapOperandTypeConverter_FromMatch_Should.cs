using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Xunit;

namespace Solitons.CommandLine;

// ReSharper disable once InconsistentNaming
public sealed class CliMapOperandTypeConverter_FromMatch_Should
{
    [Theory]
    [InlineData("--map.A 3", "A", 3)]
    [InlineData("--map[A] 3", "A", 3)]
    public void Work(string input, string key, int expectedValue)
    {
        var target = new CliMapOperandTypeConverter(typeof(Dictionary<string, int>), "test");
        var pattern = target.ToMatchPattern("--map");
        var match = Regex.Match(input, pattern);
        var map = (IDictionary)target.FromMatch(match);

        Assert.Equal(1, map.Count);
        Assert.Equal(expectedValue, map[key]);
    }
}
using System;
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
    public void HandleIntValues(string input, string key, int expectedValue)
    {
        input = CliTokenEncoder.Encode(input, out var preprocessor);
        var target = new CliMapOperandTypeConverter(typeof(Dictionary<string, int>), "test", Array.Empty<object>(), null);
        var pattern = target.ToMatchPattern("--map");
        var match = Regex.Match(input, pattern);
        var map = (IDictionary)target.FromMatch(match, preprocessor);

        Assert.Equal(1, map.Count);
        Assert.Equal(expectedValue, map[key]);
    }
}
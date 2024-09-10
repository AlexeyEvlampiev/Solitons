using System;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Solitons.CommandLine;

// ReSharper disable once InconsistentNaming
public sealed class CliSubCommand_ctor_Should
{
    [Theory]
    [InlineData("init|initialize", "init", "init|initialize")]
    [InlineData("start|begin|init", "begin", "begin|init|start")] // Test ordering of aliases
    [InlineData("commit", "commit", "commit")] // Test trimming spaces
    public void HandleAliases(
        string pattern, 
        string expectedPrimaryName, 
        string expectedSubCommandPattern)
    {
        var actualAliases = Regex
            .Split(pattern, @"\s*[|]\s*")
            .ToHashSet(StringComparer.Ordinal);
        
        var target = new CliSubCommandInfo(pattern);
        Assert.Equal(expectedPrimaryName, target.PrimaryName);
        Assert.Equal(expectedSubCommandPattern, target.SubCommandPattern);
        Assert.True(actualAliases.All(o => target.Aliases.Contains(o)));
        Assert.True(target.Aliases.All(o => actualAliases.Contains(o)));
    }


    [Theory]
    [InlineData("")] // Empty string
    [InlineData(" ")] // Whitespace only
    public void HandleEmptyOrNullPattern(string pattern)
    {
        var target = new CliSubCommandInfo(pattern);
        Assert.True(target.Aliases.Count == 1);
        Assert.Empty(target.PrimaryName);
        Assert.Equal("(?=.|$)", target.SubCommandPattern); // Default regex pattern
    }

    [Theory]
    [InlineData("init|update", "init|update")] // Ignore empty aliases due to consecutive pipes
    [InlineData("init|init|update", "init|update")] // Handle duplicate and whitespace
    public void NormalizeInput(string pattern, string expectedPattern)
    {
        var target = new CliSubCommandInfo(pattern);
        Assert.Equal(expectedPattern, target.SubCommandPattern);
    }

    [Theory]
    [InlineData("in it|update")] // Spaces within aliases
    [InlineData("init*|update")] // Invalid characters
    public void ThrowArgumentExceptionForInvalidPatterns(string pattern)
    {
        var exception = Assert.Throws<ArgumentException>(() => new CliSubCommandInfo(pattern));
        Assert.Contains(CliSubCommandInfo.ArgumentExceptionMessage, exception.Message);
    }
}
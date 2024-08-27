using System.Diagnostics;
using Moq;
using Solitons.Collections;
using Xunit;

namespace Solitons.CommandLine;

// ReSharper disable once InconsistentNaming
public sealed class ICliActionRegexMatchProvider_Rank_Should
{
    [Theory]
    [InlineData("program run arg",3, true)]
    [InlineData("program arg run",3, false)]
    [InlineData("program run run", 2, false)]
    [InlineData("program arg arg", 2, false)]
    [InlineData("program --hello", 1, false)]
    [InlineData("program --hello --world", 1, false)]
    public void HandleBasicOptionlessCommand(string commandLine, int minSuccessfulGroups, bool optimalMatchExpected)
    {
        Debug.WriteLine($"Command line: '{commandLine}'");
        Debug.WriteLine(optimalMatchExpected ? "Expected optimal match" : "No optimal match expected");
        var mock = new Mock<ICliActionRegexMatchProvider>();
        mock
            .Setup(i => i.GetCommandSegments())
            .Returns(new CliCommandSegmentData[]
            {
                new CliSubCommandData("run"),
                new CliArgumentData()
            });
        var provider = mock.Object;

        FluentArray
            .Create(0, 10, 100)
            .ForEach(optimalMatchRank =>
            {
                var expectedRank = minSuccessfulGroups + (optimalMatchExpected ? optimalMatchRank : 0);
                var actualRank = provider.Rank(commandLine, optimalMatchRank);
                Assert.Equal(expectedRank, actualRank);
            });
    }
}
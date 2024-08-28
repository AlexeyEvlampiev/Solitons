using System.Diagnostics;
using Solitons.Collections;
using Xunit;

namespace Solitons.CommandLine;

// ReSharper disable once InconsistentNaming
public sealed class CliActionSchema_Rank_Should
{
    [Theory]
    [InlineData("program run arg",2 * 2)]
    //[InlineData("program arg run",3, false)]
    //[InlineData("program run run", 2, false)]
    //[InlineData("program arg arg", 2, false)]
    //[InlineData("program --hello", 1, false)]
    //[InlineData("program --hello --world", 1, false)]
    public void HandleBasicOptionlessCommand(string commandLine, int expectedRank)
    {
        Debug.WriteLine(commandLine);
        var schema = new CliActionSchema()
            .AddSubCommand(["run"])
            .AddArgument("arg");

        FluentArray
            .Create(0, 10, 100)
            .ForEach(optimalMatchRank =>
            {
                var actualRank = schema.Rank(commandLine);
                Assert.Equal(expectedRank, actualRank);
            });
    }

    //[Theory]
    //[InlineData("program run arg --parameter.key1 value1 -p[key2] value2 --overwrite --force", 7, true)]
    //public void Work(string commandLine, int minSuccessfulGroups, bool optimalMatchExpected)
    //{
    //    Debug.WriteLine($"Command line: '{commandLine}'");
    //    Debug.WriteLine(optimalMatchExpected ? "Expected optimal match" : "No optimal match expected");
    //    var mock = new Mock<ICliActionRegexMatchProvider>();
    //    mock
    //        .Setup(i => i.GetCommandSegments())
    //        .Returns(new CliActionRegexMatchCommandSegment[]
    //        {
    //            new CliActionRegexMatchCommandToken("run"),
    //            new CliActionRegexMatchCommandArgument()
    //        });
    //    mock
    //        .Setup(i => i.GetOptions())
    //        .Returns(new CliActionRegexMatchOption[]
    //        {
    //            new CliActionRegexMatchFlagOption("overwrite", FluentArray.Create("--overwrite")),
    //            new CliActionRegexMatchFlagOption("force", FluentArray.Create("--force")),
    //            new CliActionRegexMatchMapOption("parameter", FluentArray.Create("--parameter|-p"))
    //        });
    //    var provider = mock.Object;

    //    FluentArray
    //        .Create(0, 10, 100)
    //        .ForEach(optimalMatchRank =>
    //        {
    //            var expectedRank = minSuccessfulGroups + (optimalMatchExpected ? optimalMatchRank : 0);
    //            var actualRank = provider.Rank(commandLine, optimalMatchRank);
    //            Assert.Equal(expectedRank, actualRank);
    //        });
    //}
}
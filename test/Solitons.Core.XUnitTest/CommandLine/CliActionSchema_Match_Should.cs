using System.Diagnostics;
using Xunit;

namespace Solitons.CommandLine;

// ReSharper disable once InconsistentNaming
public sealed class CliActionSchema_Match_Should
{
    [Theory]
    [InlineData("program run arg",true, 0 )]
    //[InlineData("program arg run", 1)]
    //[InlineData("program run run", 1)]
    //[InlineData("program arg arg", 0)]
    //[InlineData("program", 0)]
    //[InlineData("program --hello", 0)]
    //[InlineData("program --hello --world", 0)]
    public void HandleScenario001(string commandLine, bool success, int unrecognizedTokensCount)
    {
        Debug.WriteLine(commandLine);
        var schema = new CliActionSchema()
            .AddSubCommand(["run"])
            .AddArgument("arg");

        var match = schema.Match(commandLine);
        Assert.Equal(success, match.Success);
        var unrecognizedTokens = schema.GetUnrecognizedTokens(match);
        Assert.Equal(unrecognizedTokensCount, unrecognizedTokens.Captures.Count);
  
    }
}
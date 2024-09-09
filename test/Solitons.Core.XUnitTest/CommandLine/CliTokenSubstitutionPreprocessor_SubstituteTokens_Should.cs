using System;
using Xunit;

namespace Solitons.CommandLine;

public sealed class CliTokenSubstitutionPreprocessor_SubstituteTokens_Should
{
    [Theory]
    [InlineData("%CLI_TEST%", "CLI_TEST", "This is a test")]
    public void Work(string commandLine, string key, string expectedValue)
    {
        Environment.SetEnvironmentVariable(key, expectedValue);
        var preprocessedCommandLine = CliTokenEncoder.Encode(commandLine, out var decoder);
        var actualValue = decoder(preprocessedCommandLine);
        Assert.Equal(expectedValue, actualValue);
        Assert.NotEqual(commandLine, preprocessedCommandLine);
    }
}
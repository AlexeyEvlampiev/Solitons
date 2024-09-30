using System;
using Xunit;

namespace Solitons.CommandLine;

// ReSharper disable once InconsistentNaming
public sealed class CliTokenEncoder_SubstituteTokens_Should
{
    [Theory]
    [InlineData("%CLI_TEST%", "CLI_TEST", "This is a test", "This is a test")]
    [InlineData("%PATH%", "PATH", @"C:\Windows\System32", @"C:\Windows\System32")]
    [InlineData(@"""%TEMP%\file.txt""", "TEMP", @"C:\Users\Temp", @"C:\Users\Temp\file.txt")]
    public void SubstituteEnvironmentVariablesCorrectly(string commandLine, string key, string value, string expectedValue)
    {
        Environment.SetEnvironmentVariable(key, value);
        var preprocessedCommandLine = CliTokenEncoder.Encode(commandLine, out var decoder);
        var actualValue = decoder(preprocessedCommandLine);
        Assert.Equal(expectedValue, actualValue);
        Assert.NotEqual(commandLine, preprocessedCommandLine);
    }

    [Theory]
    [InlineData(@"%NON_EXISTENT%", "%NON_EXISTENT%")] // Variable does not exist
    [InlineData(@"%INVALID_VAR%", "%INVALID_VAR%")]   // Variable does not exist
    public void SubstituteMissingEnvironmentVariables_ShouldLeaveOriginalText(string commandLine, string expectedValue)
    {
        var preprocessedCommandLine = CliTokenEncoder.Encode(commandLine, out var decoder);
        var actualValue = decoder(preprocessedCommandLine);

        Assert.Equal(expectedValue, actualValue); // No replacement
        Assert.Equal(commandLine, preprocessedCommandLine); // Should remain the same
        }
}

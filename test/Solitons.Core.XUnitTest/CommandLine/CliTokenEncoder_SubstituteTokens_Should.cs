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

    [Theory]
    [InlineData(@"""This is quoted text""", "This is quoted text")]
    [InlineData(@"""Another quoted text with spaces""", "Another quoted text with spaces")]
    public void SubstituteQuotedTextCorrectly(string commandLine, string expectedValue)
    {
        // Act
        var preprocessedCommandLine = CliTokenEncoder.Encode(commandLine, out var decoder);
        var actualValue = decoder(preprocessedCommandLine);

        // Assert
        Assert.Equal(expectedValue, actualValue);
    }

    [Theory]
    [InlineData(@"--config[setting]", "--config.setting")]
    [InlineData(@"--config [setting]", "--config.setting")]
    [InlineData(@"--option[value]", "--option.value")]
    [InlineData(@"--option [ value ]", "--option.value")]
    public void SubstituteKeyValueIndexerOptionCorrectly(string commandLine, string expectedValue)
    {
        var preprocessedCommandLine = CliTokenEncoder.Encode(commandLine, out var decoder);
        var actualValue = decoder(preprocessedCommandLine);

        Assert.Equal(expectedValue, preprocessedCommandLine); // Encoded correctly
        Assert.Equal(expectedValue, actualValue); // Decoded should match
    }


    [Theory]
    [InlineData(@"--config.option", "--config.option")]
    [InlineData(@"--setting.value", "--setting.value")]
    [InlineData(@"--config . option", "--config.option")]
    [InlineData(@"--setting . value", "--setting.value")]
    public void SubstituteKeyValueAccessorOptionCorrectly(string commandLine, string expectedValue)
    {
        var preprocessedCommandLine = CliTokenEncoder.Encode(commandLine, out var decoder);
        var actualValue = decoder(preprocessedCommandLine);

        Assert.Equal(expectedValue, preprocessedCommandLine); // Encoded correctly
        Assert.Equal(expectedValue, actualValue); // Decoded should match
    }


    [Theory]
    [InlineData(@"""C:\Program Files\App\app.exe""", "app.exe")]
    [InlineData(@"/usr/local/bin/executable", "executable")]
    public void SubstituteProgramPathCorrectly(string commandLine, string expectedValue)
    {
        var preprocessedCommandLine = CliTokenEncoder.Encode(commandLine, out var decoder);
        var actualValue = decoder(preprocessedCommandLine);

        Assert.Equal(expectedValue, preprocessedCommandLine); // Only the file name should be kept
        Assert.Equal(expectedValue, actualValue); // Decoded should match
    }


    [Theory]
    [InlineData(@"dir ""%TEMP%\file.txt""", @"C:\Users\Temp", @"dir C:\Users\Temp\file.txt")]
    [InlineData(@"dir ""%TEMP%\folder\file.txt""", @"C:\Users\Temp", @"dir C:\Users\Temp\folder\file.txt")]
    public void SubstituteEnvironmentVariablesInPathsCorrectly(string commandLine, string tempPath, string expectedValue)
    {
        Environment.SetEnvironmentVariable("TEMP", tempPath);
        var preprocessedCommandLine = CliTokenEncoder.Encode(commandLine, out var decoder);
        var actualValue = decoder(preprocessedCommandLine);
        Assert.Equal(expectedValue, actualValue);
    }

}

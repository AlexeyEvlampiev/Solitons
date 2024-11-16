using System;
using System.Text.RegularExpressions;
using Xunit;

namespace Solitons.CommandLine;

// ReSharper disable once InconsistentNaming
public sealed class CliCommandLine_Parse_Should
{
    /// <summary>
    /// Tests that parsing a simple executable with no options or arguments correctly populates the properties.
    /// </summary>
    [Fact]
    public void Parse_SimpleExecutable_NoOptionsOrArguments_Should_Succeed()
    {
        // Arrange
        string commandLine = "app.exe";

        // Act
        CliCommandLine parsedCommand = CliCommandLine.Parse(commandLine);

        // Assert
        Assert.NotNull(parsedCommand); // Ensure that the parsedCommand is not null
        Assert.Equal("app.exe", parsedCommand.ExecutableName);
        Assert.Equal("app.exe", parsedCommand.CommandLine);
        Assert.Equal("app.exe", parsedCommand.Signature);
        Assert.Empty(parsedCommand.Segments); // No segments should be present
        Assert.Empty(parsedCommand.Options); // No options should be present

        // Additional Assertions (Optional)
        // Verify that ToString returns the original command line
        Assert.Equal("app.exe", parsedCommand.ToString());

        // Verify that the formatted signature is correct
        Assert.Equal("app.exe", parsedCommand.ToString("Signature", null));

        // Implicit string conversion
        string implicitString = parsedCommand;
        Assert.Equal("app.exe", implicitString);
    }

    /// <summary>
    /// Tests that parsing an executable with multiple arguments correctly captures all segments without any options.
    /// </summary>
    [Fact]
    public void Parse_ExecutableWithMultipleArguments_Should_CaptureAllSegments()
    {
        // Arrange
        string commandLine = "app.exe input.txt output.txt log.txt";

        // Act
        CliCommandLine parsedCommand = CliCommandLine.Parse(commandLine);

        // Assert
        Assert.NotNull(parsedCommand); // Ensure that the parsedCommand is not null
        Assert.Equal("app.exe", parsedCommand.ExecutableName);
        Assert.Equal("app.exe input.txt output.txt log.txt", parsedCommand.CommandLine);
        Assert.Equal("app.exe input.txt output.txt log.txt", parsedCommand.Signature);

        // Verify that Segments contain the correct arguments
        Assert.Equal(3, parsedCommand.Segments.Length);
        Assert.Contains("input.txt", parsedCommand.Segments);
        Assert.Contains("output.txt", parsedCommand.Segments);
        Assert.Contains("log.txt", parsedCommand.Segments);

        // Ensure that there are no options present
        Assert.Empty(parsedCommand.Options);

        // Additional Assertions (Optional)
        // Verify that ToString returns the original command line
        Assert.Equal("app.exe input.txt output.txt log.txt", parsedCommand.ToString());

        // Verify that the formatted signature is correct
        Assert.Equal("app.exe input.txt output.txt log.txt", parsedCommand.ToString("Signature", null));

        // Implicit string conversion
        string implicitString = parsedCommand;
        Assert.Equal("app.exe input.txt output.txt log.txt", implicitString);
    }

    /// <summary>
    /// Tests that parsing an executable with quoted arguments correctly captures all quoted segments.
    /// </summary>
    [Fact]
    public void Parse_ExecutableWithQuotedArguments_Should_CaptureQuotedSegments()
    {
        // Arrange
        string commandLine = @"app.exe ""C:\Program Files\input file.txt"" ""C:\Output Folder\output file.txt""";
        var signatureRegex = new Regex(@"app.exe \w{32} \w{32}");
        // Act
        CliCommandLine parsedCommand = CliCommandLine.Parse(commandLine);

        // Assert
        Assert.NotNull(parsedCommand); // Ensure that the parsedCommand is not null
        Assert.Equal("app.exe", parsedCommand.ExecutableName);
        Assert.Equal(commandLine, parsedCommand.CommandLine);
        Assert.True(signatureRegex.IsMatch(parsedCommand.Signature));
        Assert.True(signatureRegex.IsMatch(parsedCommand.ToString("Signature")));
        Assert.True(signatureRegex.IsMatch(parsedCommand.ToString("S")));

        // Verify that Segments contain the correct quoted arguments
        Assert.Equal(2, parsedCommand.Segments.Length);
        Assert.Contains(@"C:\Program Files\input file.txt", parsedCommand.Segments);
        Assert.Contains(@"C:\Output Folder\output file.txt", parsedCommand.Segments);

        // Ensure that there are no options present
        Assert.Empty(parsedCommand.Options);

        // Additional Assertions (Optional)
        // Verify that ToString returns the original command line
        Assert.Equal(commandLine, parsedCommand.ToString());



        // Implicit string conversion
        string implicitString = parsedCommand;
        Assert.Equal(commandLine, implicitString);
    }


    /// <summary>
    /// Tests that parsing an executable with environment variables correctly expands and encodes segments.
    /// </summary>
    [Fact]
    public void Parse_ExecutableWithEnvironmentVariables_Should_ExpandAndEncodeSegments()
    {
        // Arrange
        // Save original environment variables to restore them after the test
        string originalUserProfile = Environment.GetEnvironmentVariable("USERPROFILE") ?? string.Empty;
        string originalTemp = Environment.GetEnvironmentVariable("TEMP") ?? string.Empty;

        // Define test environment variable values
        string testUserProfile = @"C:\TestUser";
        string testTemp = @"C:\TestTemp";

        // Set environment variables to test values
        Environment.SetEnvironmentVariable("USERPROFILE", testUserProfile);
        Environment.SetEnvironmentVariable("TEMP", testTemp);

        // Define the command line with environment variables
        string commandLine = @"app.exe %USERPROFILE%\documents %TEMP%\output";
        var signatureRegex = new Regex(@"app\.exe \w{32}\\documents \w{32}\\output");

        try
        {
            // Act
            CliCommandLine parsedCommand = CliCommandLine.Parse(commandLine);

            // Assert
            Assert.NotNull(parsedCommand); // Ensure that the parsedCommand is not null
            Assert.Equal("app.exe", parsedCommand.ExecutableName);
            Assert.Equal(commandLine, parsedCommand.CommandLine);
            Assert.True(signatureRegex.IsMatch(parsedCommand.Signature));
            Assert.True(signatureRegex.IsMatch(parsedCommand.ToString("Signature")));
            Assert.True(signatureRegex.IsMatch(parsedCommand.ToString("S")));

            // Verify that Segments contain the correctly expanded environment variable paths
            Assert.Equal(2, parsedCommand.Segments.Length);
            Assert.Contains(@"C:\TestUser\documents", parsedCommand.Segments);
            Assert.Contains(@"C:\TestTemp\output", parsedCommand.Segments);

            // Ensure that there are no options present
            Assert.Empty(parsedCommand.Options);

            // Additional Assertions (Optional)
            // Verify that ToString returns the original command line
            Assert.Equal(commandLine, parsedCommand.ToString());


            // Implicit string conversion should return the signature
            string implicitString = parsedCommand;
            Assert.Equal(commandLine, implicitString);
        }
        finally
        {
            // Reset environment variables to their original values to avoid side effects
            Environment.SetEnvironmentVariable("USERPROFILE", originalUserProfile);
            Environment.SetEnvironmentVariable("TEMP", originalTemp);
        }
    }

    /// <summary>
    /// Tests that parsing an executable with flag options correctly captures all flag options.
    /// </summary>
    [Fact]
    public void Parse_ExecutableWithFlags_Should_CaptureFlagOptions()
    {
        // Arrange
        string commandLine = "app.exe --verbose --debug -h";

        // Act
        CliCommandLine parsedCommand = CliCommandLine.Parse(commandLine);

        // Assert
        Assert.NotNull(parsedCommand); // Ensure that the parsedCommand is not null
        Assert.Equal("app.exe", parsedCommand.ExecutableName);
        Assert.Equal("app.exe --verbose --debug -h", parsedCommand.CommandLine);
        Assert.Equal("app.exe --verbose --debug -h", parsedCommand.Signature);

        // Verify that Segments are empty since there are no arguments
        Assert.Empty(parsedCommand.Segments);

        // Ensure that Options contain exactly three flag options
        Assert.Equal(3, parsedCommand.Options.Length);

        // Verify that each option is a CliFlagOptionCapture and has the correct name
        Assert.Contains(parsedCommand.Options, option =>
            option is CliFlagOptionCapture flagOption && flagOption.Name == "--verbose");

        Assert.Contains(parsedCommand.Options, option =>
            option is CliFlagOptionCapture flagOption && flagOption.Name == "--debug");

        Assert.Contains(parsedCommand.Options, option =>
            option is CliFlagOptionCapture flagOption && flagOption.Name == "-h");

        // Additional Assertions (Optional)
        // Verify that ToString returns the original command line
        Assert.Equal("app.exe --verbose --debug -h", parsedCommand.ToString());

        // Verify that the formatted signature is correct
        Assert.Equal("app.exe --verbose --debug -h", parsedCommand.ToString("Signature", null));

        // Implicit string conversion should return the signature
        string implicitString = parsedCommand;
        Assert.Equal("app.exe --verbose --debug -h", implicitString);
    }


    /// <summary>
    /// Tests that parsing an executable with scalar options correctly captures all scalar options.
    /// </summary>
    [Fact]
    public void Parse_ExecutableWithScalarOptions_Should_CaptureScalarOptions()
    {
        // Arrange
        string commandLine = @"app.exe --output ""C:\Output Folder"" --level 5";

        // Act
        CliCommandLine parsedCommand = CliCommandLine.Parse(commandLine);

        // Assert
        Assert.NotNull(parsedCommand); // Ensure that the parsedCommand is not null
        Assert.Equal("app.exe", parsedCommand.ExecutableName);
        Assert.Equal(@"app.exe --output ""C:\Output Folder"" --level 5", parsedCommand.CommandLine);
        Assert.Equal("app.exe --output --level", parsedCommand.Signature);
        Assert.Equal("app.exe --output --level", parsedCommand.ToString("Signature"));
        Assert.Equal("app.exe --output --level", parsedCommand.ToString("S"));

        // Verify that Segments are empty since there are no arguments
        Assert.Empty(parsedCommand.Segments);

        // Ensure that Options contain exactly two scalar options
        Assert.Equal(2, parsedCommand.Options.Length);

        // Verify that each option is a CliScalarOptionCapture and has the correct name and value
        Assert.Contains(parsedCommand.Options, option =>
            option is CliScalarOptionCapture scalarOption &&
            scalarOption.Name == "--output" &&
            scalarOption.Value == @"C:\Output Folder");

        Assert.Contains(parsedCommand.Options, option =>
            option is CliScalarOptionCapture scalarOption &&
            scalarOption.Name == "--level" &&
            scalarOption.Value == "5");

        // Additional Assertions (Optional)
        // Verify that ToString returns the original command line
        Assert.Equal(@"app.exe --output ""C:\Output Folder"" --level 5", parsedCommand.ToString());

        // Implicit string conversion should return the original command line
        string implicitString = parsedCommand;
        Assert.Equal(@"app.exe --output ""C:\Output Folder"" --level 5", implicitString);
    }
}
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
        Assert.Empty(parsedCommand.Options);  // No options should be present

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
}
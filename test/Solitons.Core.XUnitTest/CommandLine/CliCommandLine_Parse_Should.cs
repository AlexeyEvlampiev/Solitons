using System;
using System.Collections.Immutable;
using System.Linq;
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


    /// <summary>
    /// Tests that parsing an executable with collection options correctly captures all collection options.
    /// </summary>
    [Fact]
    public void Parse_ExecutableWithCollectionOptions_Should_CaptureCollectionOptions()
    {
        // Arrange
        string commandLine = "app.exe --files file1.txt file2.txt file3.txt";

        // Act
        CliCommandLine parsedCommand = CliCommandLine.Parse(commandLine);

        // Assert
        Assert.NotNull(parsedCommand); // Ensure that the parsedCommand is not null
        Assert.Equal("app.exe", parsedCommand.ExecutableName);
        Assert.Equal("app.exe --files file1.txt file2.txt file3.txt", parsedCommand.CommandLine);
        Assert.Equal("app.exe --files", parsedCommand.Signature);
        

        // Verify that Segments are empty since there are no arguments
        Assert.Empty(parsedCommand.Segments);

        // Ensure that Options contain exactly one collection option
        Assert.Single(parsedCommand.Options);

        // Verify that the option is a CliCollectionOptionCapture and has the correct name and values
        var collectionOption = Assert.IsType<CliCollectionOptionCapture>(parsedCommand.Options[0]);
        Assert.Equal("--files", collectionOption.Name);
        Assert.Equivalent(new[] { "file1.txt", "file2.txt", "file3.txt" }, collectionOption.Values.ToArray());

        // Additional Assertions (Optional)
        // Verify that ToString returns the original command line
        Assert.Equal("app.exe --files file1.txt file2.txt file3.txt", parsedCommand.ToString());



        // Implicit string conversion should return the signature
        string implicitString = parsedCommand;
        Assert.Equal(commandLine, implicitString);
    }

    /// <summary>
    /// Tests that parsing an executable with keyed flag options correctly captures all keyed flag options.
    /// </summary>
    [Fact]
    public void Parse_ExecutableWithKeyedFlagOptions_Should_CaptureKeyedFlagOptions()
    {
        // Arrange
        string commandLine = "app.exe --config[env] --config[debug]";

        // Act
        CliCommandLine parsedCommand = CliCommandLine.Parse(commandLine);

        // Assert
        Assert.NotNull(parsedCommand); // Ensure that the parsedCommand is not null
        Assert.Equal("app.exe", parsedCommand.ExecutableName);
        Assert.Equal("app.exe --config[env] --config[debug]", parsedCommand.CommandLine);
        Assert.Equal("app.exe --config --config", parsedCommand.Signature);
        Assert.Equal("app.exe --config --config", parsedCommand.ToString("Signature"));
        Assert.Equal("app.exe --config --config", parsedCommand.ToString("S"));

        // Verify that Segments are empty since there are no arguments
        Assert.Empty(parsedCommand.Segments);

        // Ensure that Options contain exactly two keyed flag options
        Assert.Equal(2, parsedCommand.Options.Length);

        // Verify that each option is a CliKeyFlagOptionCapture and has the correct name and key
        Assert.Contains(parsedCommand.Options, option =>
            option is CliKeyFlagOptionCapture keyFlagOption &&
            keyFlagOption.Name == "--config" &&
            keyFlagOption.Key == "env");

        Assert.Contains(parsedCommand.Options, option =>
            option is CliKeyFlagOptionCapture keyFlagOption &&
            keyFlagOption.Name == "--config" &&
            keyFlagOption.Key == "debug");

        // Additional Assertions (Optional)
        // Verify that ToString returns the original command line
        Assert.Equal("app.exe --config[env] --config[debug]", parsedCommand.ToString());

        

        // Implicit string conversion should return the signature
        string implicitString = parsedCommand;
        Assert.Equal("app.exe --config[env] --config[debug]", implicitString);
    }

    /// <summary>
    /// Tests that parsing an executable with a quoted name containing spaces correctly captures the executable and its options.
    /// </summary>
    [Fact]
    public void Parse_ExecutableWithQuotedName_Should_CaptureQuotedExecutableAndOptions()
    {
        // Arrange
        string commandLine = @"""C:\Program Files\MyApp\app.exe"" --mode release";


        // Act
        CliCommandLine parsedCommand = CliCommandLine.Parse(commandLine);

        // Assert
        Assert.NotNull(parsedCommand); // Ensure that the parsedCommand is not null

        // Verify ExecutableName is correctly extracted without surrounding quotes
        Assert.Equal(@"app.exe", parsedCommand.ExecutableName);

        // Verify that CommandLine retains the original quotes
        Assert.Equal(commandLine, parsedCommand.CommandLine);

        // Verify that Signature replaces the option value with a placeholder
        Assert.Equal(@"app.exe --mode", parsedCommand.Signature);

        // Verify that Segments are empty since there are no standalone arguments
        Assert.Empty(parsedCommand.Segments);

        // Ensure that Options contain exactly one scalar option
        Assert.Single(parsedCommand.Options);

        // Retrieve and verify the scalar option
        var scalarOption = Assert.IsType<CliScalarOptionCapture>(parsedCommand.Options[0]);
        Assert.Equal("--mode", scalarOption.Name);
        Assert.Equal("release", scalarOption.Value);

        // Additional Assertions (Optional)
        // Verify that ToString returns the original command line
        Assert.Equal(commandLine, parsedCommand.ToString());

        // Verify that the formatted signature is correct
        Assert.Equal(@"app.exe --mode", parsedCommand.ToString("Signature"));

        // Implicit string conversion should return the signature
        string implicitString = parsedCommand;
        Assert.Equal(commandLine, implicitString);
    }

    /// <summary>
    /// Tests that parsing an executable with mixed positional arguments and options correctly captures both.
    /// </summary>
    [Fact]
    public void Parse_ExecutableWithMixedArgumentsAndOptions_Should_CaptureCorrectly()
    {
        // Arrange
        string commandLine = @"app.exe input.txt --verbose --output ""C:\Output Folder""";

        // Act
        CliCommandLine parsedCommand = CliCommandLine.Parse(commandLine);

        // Assert
        Assert.NotNull(parsedCommand); // Ensure that the parsedCommand is not null

        // Verify ExecutableName is correctly extracted
        Assert.Equal("app.exe", parsedCommand.ExecutableName);

        // Verify that CommandLine retains the original input
        Assert.Equal(commandLine, parsedCommand.CommandLine);

        // Verify that Signature replaces the scalar option value with a placeholder
        Assert.Equal(@"app.exe input.txt --verbose --output", parsedCommand.Signature);
        Assert.Equal(@"app.exe input.txt --verbose --output", parsedCommand.ToString("Signature"));
        Assert.Equal(@"app.exe input.txt --verbose --output", parsedCommand.ToString("S"));

        // Verify that Segments contain the positional argument
        Assert.Single(parsedCommand.Segments);
        Assert.Contains("input.txt", parsedCommand.Segments);

        // Ensure that Options contain exactly two options
        Assert.Equal(2, parsedCommand.Options.Length);

        // Verify that --verbose is captured as a CliFlagOptionCapture
        Assert.Contains(parsedCommand.Options, option =>
            option is CliFlagOptionCapture flagOption &&
            flagOption.Name == "--verbose");

        // Verify that --output is captured as a CliScalarOptionCapture with the correct value
        Assert.Contains(parsedCommand.Options, option =>
            option is CliScalarOptionCapture scalarOption &&
            scalarOption.Name == "--output" &&
            scalarOption.Value == @"C:\Output Folder");

        // Additional Assertions (Optional)
        // Verify that ToString returns the original command line
        Assert.Equal(commandLine, parsedCommand.ToString());



        // Implicit string conversion should return the signature
        string implicitString = parsedCommand;
        Assert.Equal(commandLine, implicitString);
    }

    /// <summary>
    /// Tests that parsing an executable with subcommands and their options correctly captures both.
    /// </summary>
    [Fact]
    public void Parse_ExecutableWithSubcommandsAndOptions_Should_CaptureSubcommandsAndTheirOptions()
    {
        // Arrange
        string commandLine = @"app.exe deploy --environment production --force";

        // Act
        CliCommandLine parsedCommand = CliCommandLine.Parse(commandLine);

        // Assert
        Assert.NotNull(parsedCommand); // Ensure that the parsedCommand is not null

        // Verify ExecutableName is correctly extracted
        Assert.Equal("app.exe", parsedCommand.ExecutableName);

        Assert.Equal(1, parsedCommand.Segments.Length);
        // Verify Subcommand is correctly identified
        Assert.Equal("deploy", parsedCommand.Segments[0]);

        // Verify that CommandLine retains the original input
        Assert.Equal(@"app.exe deploy --environment production --force", parsedCommand.CommandLine);

        // Verify that Signature replaces the scalar option value with a placeholder
        Assert.Equal("app.exe deploy --environment --force", parsedCommand.Signature);
        Assert.Equal("app.exe deploy --environment --force", parsedCommand.ToString("Signature"));
        Assert.Equal("app.exe deploy --environment --force", parsedCommand.ToString("S"));


        // Ensure that Options contain exactly two options
        Assert.Equal(2, parsedCommand.Options.Length);

        // Verify that --environment is captured as a CliScalarOptionCapture with the correct value
        Assert.Contains(parsedCommand.Options, option =>
            option is CliScalarOptionCapture scalarOption &&
            scalarOption.Name == "--environment" &&
            scalarOption.Value == "production");

        // Verify that --force is captured as a CliFlagOptionCapture
        Assert.Contains(parsedCommand.Options, option =>
            option is CliFlagOptionCapture flagOption &&
            flagOption.Name == "--force");

        // Additional Assertions (Optional)
        // Verify that ToString returns the original command line
        Assert.Equal(@"app.exe deploy --environment production --force", parsedCommand.ToString());


        // Implicit string conversion should return the signature
        string implicitString = parsedCommand;
        Assert.Equal(commandLine, implicitString);
    }

    /// <summary>
    /// Tests that parsing an empty command line throws an ArgumentOutOfRangeException.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Parse_EmptyCommandLine_Should_ThrowArgumentNullException(string commandLined)
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => CliCommandLine.Parse(commandLined));
    }

    /// <summary>
    /// Tests that parsing a command line with special characters correctly captures them.
    /// </summary>
    [Fact]
    public void Parse_CommandLineWithSpecialCharacters_Should_CaptureSpecialCharacters()
    {
        // Arrange
        string commandLine = @"app.exe --path C:\Path\With\Special\!@#$%^&*() chars";

        // Act
        CliCommandLine parsedCommand = CliCommandLine.Parse(commandLine);

        // Assert
        Assert.NotNull(parsedCommand);
        Assert.Equal("app.exe", parsedCommand.ExecutableName);
        Assert.Equal(@"app.exe --path C:\Path\With\Special\!@#$%^&*() chars", parsedCommand.CommandLine);
        Assert.Equal("app.exe --path", parsedCommand.Signature);

        Assert.Single(parsedCommand.Options);
        var collectionOption = Assert.IsType<CliCollectionOptionCapture>(parsedCommand.Options[0]);
        Assert.Equal("--path", collectionOption.Name);
        Assert.Equal(2, collectionOption.Values.Length);
        Assert.True(collectionOption.Values.Contains(@"C:\Path\With\Special\!@#$%^&*()"));
        Assert.True(collectionOption.Values.Contains(@"chars"));
    }


    /// <summary>
    /// Tests that parsing a command line with duplicate options aggregates their values.
    /// </summary>
    [Fact]
    public void Parse_CommandLineWithDuplicateOptions_Should_AggregateValues()
    {
        // Arrange
        string commandLine = "app.exe --include file1.txt --include file2.txt";

        // Act
        CliCommandLine parsedCommand = CliCommandLine.Parse(commandLine);

        // Assert
        Assert.NotNull(parsedCommand);
        Assert.Equal("app.exe", parsedCommand.ExecutableName);
        Assert.Equal("app.exe --include file1.txt --include file2.txt", parsedCommand.CommandLine);
        Assert.Equal("app.exe --include --include", parsedCommand.Signature);

        Assert.Equal(2, parsedCommand.Options.Length);
        Assert.All(parsedCommand.Options, option =>
        {
            Assert.IsType<CliScalarOptionCapture>(option);
            Assert.Equal("--include", option.Name);
        });

        var includeOptions = parsedCommand.Options.Cast<CliScalarOptionCapture>().ToList();
        Assert.Contains(includeOptions, option => option.Value == "file1.txt");
        Assert.Contains(includeOptions, option => option.Value == "file2.txt");
    }


    /// <summary>
    /// Tests that parsing a command line with nested subcommands and their options correctly captures all components.
    /// </summary>
    [Fact]
    public void Parse_CommandLineWithNestedSubcommandsAndOptions_Should_CaptureAllComponents()
    {
        // Arrange
        string commandLine = @"app.exe service start --force --timeout 30";

        // Act
        CliCommandLine parsedCommand = CliCommandLine.Parse(commandLine);

        // Assert
        Assert.NotNull(parsedCommand);
        Assert.Equal("app.exe", parsedCommand.ExecutableName);
        Assert.Equal(@"app.exe service start --force --timeout 30", parsedCommand.CommandLine);
        Assert.Equal(@"app.exe service start --force --timeout", parsedCommand.Signature);

        // Verify segments (subcommands)
        Assert.Equal(2, parsedCommand.Segments.Length);
        Assert.Contains("service", parsedCommand.Segments);
        Assert.Contains("start", parsedCommand.Segments);

        // Verify options
        Assert.Equal(2, parsedCommand.Options.Length);
        var forceOption = Assert.IsType<CliFlagOptionCapture>(parsedCommand.Options[0]);
        Assert.Equal("--force", forceOption.Name);

        var timeoutOption = Assert.IsType<CliScalarOptionCapture>(parsedCommand.Options[1]);
        Assert.Equal("--timeout", timeoutOption.Name);
        Assert.Equal("30", timeoutOption.Value);
    }


    /// <summary>
    /// Tests that parsing a command line with Unicode characters correctly captures them.
    /// </summary>
    [Fact]
    public void Parse_CommandLineWithUnicodeCharacters_Should_CaptureUnicodeCharacters()
    {
        // Arrange
        string commandLine = @"app.exe --message ""こんにちは世界"" --path C:\データ";

        // Act
        CliCommandLine parsedCommand = CliCommandLine.Parse(commandLine);

        // Assert
        Assert.NotNull(parsedCommand);
        Assert.Equal("app.exe", parsedCommand.ExecutableName);
        Assert.Equal(@"app.exe --message ""こんにちは世界"" --path C:\データ", parsedCommand.CommandLine);
        Assert.Equal("app.exe --message --path", parsedCommand.Signature);

        Assert.Equal(2, parsedCommand.Options.Length);

        var messageOption = Assert.IsType<CliScalarOptionCapture>(parsedCommand.Options[0]);
        Assert.Equal("--message", messageOption.Name);
        Assert.Equal("こんにちは世界", messageOption.Value);

        var pathOption = Assert.IsType<CliScalarOptionCapture>(parsedCommand.Options[1]);
        Assert.Equal("--path", pathOption.Name);
        Assert.Equal(@"C:\データ", pathOption.Value);
    }


    /// <summary>
    /// Tests that parsing a command line with mixed option types correctly captures all options.
    /// </summary>
    [Fact]
    public void Parse_CommandLineWithMixedOptionTypes_Should_CaptureAllOptionTypes()
    {
        // Arrange
        string commandLine = @"app.exe --enable-feature --set-level 3 --tags tag1 tag2 tag3 --config[env] production";

        // Act
        CliCommandLine parsedCommand = CliCommandLine.Parse(commandLine);

        // Assert
        Assert.NotNull(parsedCommand);
        Assert.Equal("app.exe", parsedCommand.ExecutableName);
        Assert.Equal(@"app.exe --enable-feature --set-level 3 --tags tag1 tag2 tag3 --config[env] production", parsedCommand.CommandLine);
        Assert.Equal("app.exe --enable-feature --set-level --tags --config", parsedCommand.Signature);

        Assert.Equal(4, parsedCommand.Options.Length);

        var enableFeatureOption = Assert.IsType<CliFlagOptionCapture>(parsedCommand.Options[0]);
        Assert.Equal("--enable-feature", enableFeatureOption.Name);

        var setLevelOption = Assert.IsType<CliScalarOptionCapture>(parsedCommand.Options[1]);
        Assert.Equal("--set-level", setLevelOption.Name);
        Assert.Equal("3", setLevelOption.Value);

        var tagsOption = Assert.IsType<CliCollectionOptionCapture>(parsedCommand.Options[2]);
        Assert.Equal("--tags", tagsOption.Name);
        Assert.Equivalent(new[] { "tag1", "tag2", "tag3" }, tagsOption.Values.ToArray());

        var configOption = Assert.IsType<CliKeyValueOptionCapture>(parsedCommand.Options[3]);
        Assert.Equal("--config", configOption.Name);
        Assert.Equal("env", configOption.Key);
        Assert.Equal("production", configOption.Value);
    }

    /// <summary>
    /// Tests that parsing a command line with nested keyed options correctly captures all keys and their values.
    /// </summary>
    [Fact]
    public void Parse_CommandLineWithNestedKeyedOptions_Should_CaptureAllKeysAndValues()
    {
        // Arrange
        string commandLine = "app.exe --database[host] localhost --database[port] 5432 --database[user] admin";

        // Act
        CliCommandLine parsedCommand = CliCommandLine.Parse(commandLine);

        // Assert
        Assert.NotNull(parsedCommand);
        Assert.Equal("app.exe", parsedCommand.ExecutableName);
        Assert.Equal("app.exe --database[host] localhost --database[port] 5432 --database[user] admin", parsedCommand.CommandLine);
        Assert.Equal("app.exe --database --database --database", parsedCommand.Signature);

        Assert.Equal(3, parsedCommand.Options.Length);

        var hostOption = Assert.IsType<CliKeyValueOptionCapture>(parsedCommand.Options[0]);
        Assert.Equal("--database", hostOption.Name);
        Assert.Equal("host", hostOption.Key);
        Assert.Equal("localhost", hostOption.Value);

        var portOption = Assert.IsType<CliKeyValueOptionCapture>(parsedCommand.Options[1]);
        Assert.Equal("--database", portOption.Name);
        Assert.Equal("port", portOption.Key);
        Assert.Equal("5432", portOption.Value);

        var userOption = Assert.IsType<CliKeyValueOptionCapture>(parsedCommand.Options[2]);
        Assert.Equal("--database", userOption.Name);
        Assert.Equal("user", userOption.Key);
        Assert.Equal("admin", userOption.Value);
    }



    /// <summary>
    /// Tests that parsing a command line with multiple keyed options and their collections correctly captures all keys and values.
    /// </summary>
    [Fact]
    public void Parse_CommandLineWithMultipleKeyedOptionsAndCollections_Should_CaptureAllKeysAndValues()
    {
        // Arrange
        string commandLine = "app.exe --config[env] production --config[servers] server1 server2 server3";

        // Act
        CliCommandLine parsedCommand = CliCommandLine.Parse(commandLine);

        // Assert
        Assert.NotNull(parsedCommand);
        Assert.Equal("app.exe", parsedCommand.ExecutableName);
        Assert.Equal("app.exe --config[env] production --config[servers] server1 server2 server3", parsedCommand.CommandLine);
        Assert.Equal("app.exe --config --config", parsedCommand.Signature);

        Assert.Equal(2, parsedCommand.Options.Length);

        var envOption = Assert.IsType<CliKeyValueOptionCapture>(parsedCommand.Options[0]);
        Assert.Equal("--config", envOption.Name);
        Assert.Equal("env", envOption.Key);
        Assert.Equal("production", envOption.Value);

        var serversOption = Assert.IsType<CliKeyCollectionOptionCapture>(parsedCommand.Options[1]);
        Assert.Equal("--config", serversOption.Name);
        Assert.Equal("servers", serversOption.Key);
        Assert.Equivalent(new[] { "server1", "server2", "server3" }, serversOption.Values.ToArray());
    }
}
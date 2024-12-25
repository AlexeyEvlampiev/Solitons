using System;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Solitons.CommandLine;

// ReSharper disable once InconsistentNaming
public sealed class CliCommandLine_FromArgs_Should
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
        CliCommandLine parsedCommand = CliCommandLine.FromArgs(commandLine);

        // Assert
        Assert.NotNull(parsedCommand); // Ensure that the parsedCommand is not null
        Assert.Equal("app.exe", parsedCommand.ExecutableName);
        Assert.Equal("app.exe", parsedCommand.CommandLine);
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
        CliCommandLine parsedCommand = CliCommandLine.FromArgs(commandLine);

        // Assert
        Assert.NotNull(parsedCommand); // Ensure that the parsedCommand is not null
        Assert.Equal("app.exe", parsedCommand.ExecutableName);
        Assert.Equal("app.exe input.txt output.txt log.txt", parsedCommand.CommandLine);


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
        CliCommandLine parsedCommand = CliCommandLine.FromArgs(commandLine);

        // Assert
        Assert.NotNull(parsedCommand); // Ensure that the parsedCommand is not null
        Assert.Equal("app.exe", parsedCommand.ExecutableName);


        // Verify that Segments contain the correct quoted arguments
        Assert.Equal(2, parsedCommand.Segments.Length);
        Assert.Contains(@"C:\Program Files\input file.txt", parsedCommand.Segments);
        Assert.Contains(@"C:\Output Folder\output file.txt", parsedCommand.Segments);

        // Ensure that there are no options present
        Assert.Empty(parsedCommand.Options);

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
            CliCommandLine parsedCommand = CliCommandLine.FromArgs(commandLine);

            // Assert
            Assert.NotNull(parsedCommand); // Ensure that the parsedCommand is not null
            Assert.Equal("app.exe", parsedCommand.ExecutableName);

            // Verify that Segments contain the correctly expanded environment variable paths
            Assert.Equal(2, parsedCommand.Segments.Length);
            Assert.Contains(@"C:\TestUser\documents", parsedCommand.Segments);
            Assert.Contains(@"C:\TestTemp\output", parsedCommand.Segments);

            // Ensure that there are no options present
            Assert.Empty(parsedCommand.Options);

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
        CliCommandLine parsedCommand = CliCommandLine.FromArgs(commandLine);

        // Assert
        Assert.NotNull(parsedCommand); // Ensure that the parsedCommand is not null
        Assert.Equal("app.exe", parsedCommand.ExecutableName);
        Assert.Equal("app.exe --verbose --debug -h", parsedCommand.CommandLine);

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
        CliCommandLine parsedCommand = CliCommandLine.FromArgs(commandLine);

        // Assert
        Assert.NotNull(parsedCommand); // Ensure that the parsedCommand is not null
        Assert.Equal("app.exe", parsedCommand.ExecutableName);

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
        CliCommandLine parsedCommand = CliCommandLine.FromArgs(commandLine);

        // Assert
        Assert.NotNull(parsedCommand); // Ensure that the parsedCommand is not null
        Assert.Equal("app.exe", parsedCommand.ExecutableName);
        

        // Verify that Segments are empty since there are no arguments
        Assert.Empty(parsedCommand.Segments);

        // Ensure that Options contain exactly one collection option
        Assert.Single(parsedCommand.Options);

        // Verify that the option is a CliCollectionOptionCapture and has the correct name and values
        var collectionOption = Assert.IsType<CliCollectionOptionCapture>(parsedCommand.Options[0]);
        Assert.Equal("--files", collectionOption.Name);
        Assert.Equivalent(new[] { "file1.txt", "file2.txt", "file3.txt" }, collectionOption.Values.ToArray());

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
        CliCommandLine parsedCommand = CliCommandLine.FromArgs(commandLine);

        // Assert
        Assert.NotNull(parsedCommand); // Ensure that the parsedCommand is not null
        Assert.Equal("app.exe", parsedCommand.ExecutableName);

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
        CliCommandLine parsedCommand = CliCommandLine.FromArgs(commandLine);

        // Assert
        Assert.NotNull(parsedCommand); // Ensure that the parsedCommand is not null

        // Verify ExecutableName is correctly extracted without surrounding quotes
        Assert.Equal(@"app.exe", parsedCommand.ExecutableName);

        // Verify that Signature replaces the option value with a placeholder

        // Verify that Segments are empty since there are no standalone arguments
        Assert.Empty(parsedCommand.Segments);

        // Ensure that Options contain exactly one scalar option
        Assert.Single(parsedCommand.Options);

        // Retrieve and verify the scalar option
        var scalarOption = Assert.IsType<CliScalarOptionCapture>(parsedCommand.Options[0]);
        Assert.Equal("--mode", scalarOption.Name);
        Assert.Equal("release", scalarOption.Value);
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
        CliCommandLine parsedCommand = CliCommandLine.FromArgs(commandLine);

        // Assert
        Assert.NotNull(parsedCommand); // Ensure that the parsedCommand is not null

        // Verify ExecutableName is correctly extracted
        Assert.Equal("app.exe", parsedCommand.ExecutableName);


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
        CliCommandLine parsedCommand = CliCommandLine.FromArgs(commandLine);

        // Assert
        Assert.NotNull(parsedCommand); // Ensure that the parsedCommand is not null

        // Verify ExecutableName is correctly extracted
        Assert.Equal("app.exe", parsedCommand.ExecutableName);

        Assert.Equal(1, parsedCommand.Segments.Length);
        // Verify Subcommand is correctly identified
        Assert.Equal("deploy", parsedCommand.Segments[0]);




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
        Assert.Throws<ArgumentOutOfRangeException>(() => CliCommandLine.FromArgs(commandLined));
    }

    /// <summary>
    /// Tests that parsing a command line with special characters correctly captures them.
    /// </summary>
    [Fact]
    public void Parse_CommandLineWithSpecialCharacters_Should_CaptureSpecialCharacters()
    {
        // Arrange
        string commandLineText = @"app.exe --path C:\Path\With\Special\!@#$%^&*() chars";

        // Act
        CliCommandLine commandLine = CliCommandLine.FromArgs(commandLineText);

        // Assert
        Assert.NotNull(commandLine);
        Assert.Equal("app.exe", commandLine.ExecutableName);

        Assert.Single(commandLine.Options);
        var collectionOption = Assert.IsType<CliCollectionOptionCapture>(commandLine.Options[0]);
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
        string commandLineText = "app.exe --include file1.txt --include file2.txt";

        // Act
        var commandLine = CliCommandLine.FromArgs(commandLineText);

        // Assert
        Assert.NotNull(commandLine);
        Assert.Equal("app.exe", commandLine.ExecutableName);

        Assert.Equal(2, commandLine.Options.Length);
        Assert.All(commandLine.Options, option =>
        {
            Assert.IsType<CliScalarOptionCapture>(option);
            Assert.Equal("--include", option.Name);
        });

        var includeOptions = commandLine.Options.Cast<CliScalarOptionCapture>().ToList();
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
        CliCommandLine parsedCommand = CliCommandLine.FromArgs(commandLine);

        // Assert
        Assert.NotNull(parsedCommand);
        Assert.Equal("app.exe", parsedCommand.ExecutableName);
        Assert.Equal(@"app.exe service start --force --timeout 30", parsedCommand.CommandLine);

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
        CliCommandLine parsedCommand = CliCommandLine.FromArgs(commandLine);

        // Assert
        Assert.NotNull(parsedCommand);
        Assert.Equal("app.exe", parsedCommand.ExecutableName);


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
        CliCommandLine parsedCommand = CliCommandLine.FromArgs(commandLine);

        // Assert
        Assert.NotNull(parsedCommand);
        Assert.Equal("app.exe", parsedCommand.ExecutableName);
       

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
        CliCommandLine parsedCommand = CliCommandLine.FromArgs(commandLine);

        // Assert
        Assert.NotNull(parsedCommand);
        Assert.Equal("app.exe", parsedCommand.ExecutableName);
        Assert.Equal("app.exe --database[host] localhost --database[port] 5432 --database[user] admin", parsedCommand.CommandLine);

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
        CliCommandLine parsedCommand = CliCommandLine.FromArgs(commandLine);

        // Assert
        Assert.NotNull(parsedCommand);
        Assert.Equal("app.exe", parsedCommand.ExecutableName);

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


    [Fact]
    public void Parse_SimpleCommand_ExtractsExecutableName()
    {
        // Arrange
        var commandLine = "dotnet.exe build";

        // Act
        var result = CliCommandLine.FromArgs(commandLine);

        // Assert
        Assert.Equal("dotnet.exe", result.ExecutableName);
        Assert.Single(result.Segments, "build");
        Assert.Empty(result.Options);
    }



    [Fact]
    public void Parse_QuotedExecutableName_HandlesCorrectly()
    {
        // Arrange
        var commandLine = "\"My Program.exe\" arg1 arg2";

        // Act
        var result = CliCommandLine.FromArgs(commandLine);

        // Assert
        Assert.Equal("My Program.exe", result.ExecutableName);
        Assert.Equal(new[] { "arg1", "arg2" }, result.Segments);
    }

    [Fact]
    public void Parse_EnvironmentVariables_ResolvesCorrectly()
    {
        // Arrange
        Environment.SetEnvironmentVariable("TEST_VAR", "test-value");
        var commandLine = "app.exe %TEST_VAR%";

        // Act
        var result = CliCommandLine.FromArgs(commandLine);

        // Assert
        Assert.Equal("app.exe", result.ExecutableName);
        Assert.Single(result.Segments, "test-value");
    }


    [Theory]
    [InlineData("app.exe --flag", typeof(CliFlagOptionCapture))]
    [InlineData("app.exe --option value", typeof(CliScalarOptionCapture))]
    [InlineData("app.exe --list val1 val2", typeof(CliCollectionOptionCapture))]
    public void Parse_DifferentOptionTypes_CapturedCorrectly(string commandLine, Type expectedType)
    {
        // Act
        var result = CliCommandLine.FromArgs(commandLine);

        // Assert
        Assert.Single(result.Options);
        Assert.IsType(expectedType, result.Options[0]);
    }

    [Fact]
    public void Parse_KeyedOptions_CapturedCorrectly()
    {
        // Arrange
        var commandLine = "app.exe --config[env] prod --settings[log] debug info warn";

        // Act
        var result = CliCommandLine.FromArgs(commandLine);

        // Assert
        Assert.Equal(2, result.Options.Length);

        var configOption = Assert.IsType<CliKeyValueOptionCapture>(result.Options[0]);
        Assert.Equal("--config", configOption.Name);
        Assert.Equal("env", configOption.Key);
        Assert.Equal("prod", configOption.Value);

        var settingsOption = Assert.IsType<CliKeyCollectionOptionCapture>(result.Options[1]);
        Assert.Equal("--settings", settingsOption.Name);
        Assert.Equal("log", settingsOption.Key);
        Assert.Equal(new[] { "debug", "info", "warn" }, settingsOption.Values);
    }

    [Fact]
    public void Parse_ComplexCommand_HandlesAllFeatures()
    {
        // Arrange
        var commandLine = @"tool.exe subcommand ""quoted arg"" --flag --scalar value --list item1 item2 --config[env] prod --multi[tags] tag1 tag2";

        // Act
        var result = CliCommandLine.FromArgs(commandLine);

        // Assert
        Assert.Equal("tool.exe", result.ExecutableName);
        Assert.Equal(new[] { "subcommand", "quoted arg" }, result.Segments);
        Assert.Equal(5, result.Options.Length);

        Assert.IsType<CliFlagOptionCapture>(result.Options[0]);
        Assert.IsType<CliScalarOptionCapture>(result.Options[1]);
        Assert.IsType<CliCollectionOptionCapture>(result.Options[2]);
        Assert.IsType<CliKeyValueOptionCapture>(result.Options[3]);
        Assert.IsType<CliKeyCollectionOptionCapture>(result.Options[4]);
    }
}
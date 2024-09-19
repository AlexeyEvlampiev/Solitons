using System;
using System.Collections.Generic;
using Moq;
using Xunit;

namespace Solitons.CommandLine;

// ReSharper disable once InconsistentNaming
public class CliProcessor_Process_Should
{
    [Theory]
    [InlineData("test-cli -?", 0)]
    [InlineData("test-cli -h", 0)]
    [InlineData("test-cli --help", 0)]
    public void ShowGeneralHelp(string commandLine, int expectedExitCode)
    {
        var processor = new Mock<ICliProcessor>();
        int exitCode = processor.Object.Process(commandLine);
        processor.Verify(m => m
            .ShowGeneralHelp("test-cli"), Times.Once());
        processor.Verify(m => m
            .ShowHelpFor(It.IsAny<string>(), It.IsAny<CliTokenDecoder>()), Times.Never());
        Assert.Equal(expectedExitCode, exitCode);
    }

    [Theory]
    [InlineData("test-cli run --help", 0)]
    [InlineData("test-cli run", 1)]
    public void ShowTargetedHelp(string commandLine, int expectedExitCode)
    {
        var processor = new Mock<ICliProcessor>();

        int exitCode = processor.Object.Process(commandLine);
        processor.Verify(m => m
            .ShowHelpFor(commandLine, It.IsAny<CliTokenDecoder>()), Times.Once());
        processor.Verify(m => m
            .ShowGeneralHelp(It.IsAny<string>()), Times.Never());
        Assert.Equal(expectedExitCode, exitCode);
    }

    [Theory]
    [InlineData(1,1, 2)]
    [InlineData(1, 100, 101)]
    public void ReturnActionExitCode(int a, int b, int expectedSum)
    {
        var processor = ICliProcessor
            .Setup(options => options
                .UseCommandsFrom(this));
        var sum = processor.Process($"tool sum {a} {b}");
        Assert.Equal(expectedSum, sum);

        sum = processor.Process($"tool sum -a {a} -b {b}");
        Assert.Equal(expectedSum, sum);
    }

    [Fact]
    public void HandleMapOptions()
    {
        var exitCode = ICliProcessor
            .Setup(options => options
                .UseCommandsFrom(this))
            .Process("tool use maps -cs.a 1 -ci[b] 2");
        Assert.Equal(0, exitCode);
    }

    [CliRoute("run")]
    public void VoidAction()
    {
    }

    [CliRoute("sum")]
    [CliRouteArgument(nameof(a), "param a")]
    [CliRouteArgument(nameof(b), "param b")]
    public int SumActionUsingArguments(int a, int b) => a + b;

    [CliRoute("sum")]
    public int SumActionUsingOptions(
        [CliOption("-a")]int a,
        [CliOption("-b")] int b) => a + b;

    [CliRoute("use maps")]
    public int UseMaps(
        [CliOption("-cs")]Dictionary<string, int> defaultMap,
        [CliCaseSensitiveMapOption("-ci")] Dictionary<string, int> customizedMap)
    {
        Assert.True(defaultMap.Comparer.Equals(StringComparer.OrdinalIgnoreCase));
        Assert.True(customizedMap.Comparer.Equals(StringComparer.Ordinal));
        return 0;
    }

    sealed class CliCaseSensitiveMapOptionAttribute(string specification, string description = "")
        : CliOptionAttribute(specification, description)
    {
        public override StringComparer GetValueComparer() => StringComparer.Ordinal;
    }
}
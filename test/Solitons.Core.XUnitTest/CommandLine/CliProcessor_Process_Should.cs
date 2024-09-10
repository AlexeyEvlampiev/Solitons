using System;
using System.Collections.Generic;
using Moq;
using Xunit;

namespace Solitons.CommandLine;

// ReSharper disable once InconsistentNaming
public class CliProcessor_Process_Should
{
    [Theory]
    [InlineData("test-cli", 1, 1)]
    [InlineData("test-cli -?", 0, 1)]
    [InlineData("test-cli -h", 0, 1)]
    [InlineData("test-cli --help", 0, 1)]
    [InlineData("test-cli run --help", 0, 1)]
    [InlineData("test-cli run", 0, 0)]
    public void RespondToHelpFlag(string commandLine, int expectedExitCode, int showHelpCalledCount)
    {
        var callback = new Mock<ICliProcessorCallback>();
        
        var processor = CliProcessor
            .Setup(options => options
                .UseCommandsFrom(this)
                .UseCallback(callback.Object));
        
        var showHelpCalled = showHelpCalledCount switch
        {
            0 => Times.Never(),
            1 => Times.Once(),
            _ => throw new NotSupportedException()
        };

        int exitCode = processor.Process(commandLine);
        callback.Verify(m => m.ShowHelp(It.IsAny<string>()), showHelpCalled);
        callback.Verify(m => m.ShowHelp(commandLine), showHelpCalled);
        Assert.Equal(expectedExitCode, exitCode);
    }

    [Theory]
    [InlineData(1,1, 2)]
    [InlineData(1, 100, 101)]
    public void ReturnActionExitCode(int a, int b, int expectedSum)
    {
        var processor = CliProcessor
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
        var exitCode = CliProcessor
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
        [CliCaseInsensitiveMapOption("-ci")] Dictionary<string, int> customizedMap)
    {
        var defaultComparer = new Dictionary<string, int>().Comparer;
        Assert.True(defaultMap.Comparer.Equals(defaultComparer));
        Assert.True(customizedMap.Comparer.Equals(StringComparer.OrdinalIgnoreCase));
        return 0;
    }

    sealed class CliCaseInsensitiveMapOptionAttribute(string specification, string description = "")
        : CliOptionAttribute(specification, description), ICliMapOption
    {
        public StringComparer GetComparer() => StringComparer.OrdinalIgnoreCase;
    }
}
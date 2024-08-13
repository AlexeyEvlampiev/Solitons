using System;
using Moq;
using Xunit;

namespace Solitons.CommandLine;

public class CliProcessor_Should
{
    [Theory]
    [InlineData("test-cli", 1, 1)]
    [InlineData("test-cli -?", 0, 1)]
    [InlineData("test-cli -h", 0, 1)]
    [InlineData("test-cli --help", 0, 1)]
    [InlineData("test-cli run --help", 0, 1)]
    [InlineData("test-cli run", 0, 0)]
    public void Work(string commandLine, int expectedExitCode, int showHelpCalledCount)
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

    [CliCommand("run")]
    public void TestAction()
    {

    }
}
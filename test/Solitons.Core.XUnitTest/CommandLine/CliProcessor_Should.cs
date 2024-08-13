using Moq;
using Xunit;

namespace Solitons.CommandLine;

public class CliProcessor_Should
{
    [Fact]
    public void Work()
    {
        var callback = new Mock<ICliProcessorCallback>();
        
        var processor = CliProcessor
            .Setup(options => options
                .UseCommandsFrom(this)
                .UseCallback(callback.Object));

        int exitCode = processor.Process("tool");
        callback.Verify(m => m.ShowHelp("tool"), Times.Once);
        Assert.Equal(1, exitCode);

        exitCode = processor.Process("tool -?");
        callback.Verify(m => m.ShowHelp("tool"), Times.Once);
        Assert.Equal(0, exitCode);
    }

    [CliCommand("test")]
    public void TestAction()
    {

    }
}
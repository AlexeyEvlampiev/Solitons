using Moq;
using Solitons.CommandLine;

namespace Solitons.Postgres.PgUp;

// ReSharper disable once InconsistentNaming
public class Program_Initialize_Should
{
    [Theory]
    [InlineData("pgup init . --template basic", "basic")]
    public void BeInvokedByCliProcessor(string commandLine, string template)
    {
        var program = new Mock<IPgUpCliContract>();
        var processor = ICliProcessor
            .Setup(config => config
                .UseCommandsFrom(program.Object));

        processor.Process(commandLine);
        program.Verify(m => m.Initialize(".", template), Times.Once);
    }
}
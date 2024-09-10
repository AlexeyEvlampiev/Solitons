using Moq;
using Solitons.CommandLine;

namespace Solitons.Postgres.PgUp;

public class Program_Initialize_Should
{
    [Theory]
    [InlineData("pgup init . --template basic", "basic")]
    public void BeInvokedByCliProcessor(string commandLine, string template)
    {
        var program = new Mock<IPgUpProgram>();
        var processor = CliProcessor
            .Setup(config => config
                .UseCommandsFrom(new Program(program.Object)));

        processor.Process(commandLine);
        program.Verify(m => m.Initialize(".", template), Times.Once);
    }
}
using System.Diagnostics;
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
        var program = new Mock<IPgUpProgram>();
        var processor = CliProcessor
            .Setup(config => config
                .UseCommandsFrom(program.Object));

        processor.Process(commandLine);
        program.Verify(m => m.Initialize(".", template), Times.Once);
    }

    [Fact]
    public void Work()
    {
        var examples = typeof(IPgUpProgram)
            .GetMethods()
            .SelectMany(mi => mi.GetCustomAttributes(true)
                .OfType<CliCommandExampleAttribute>())
            .ToArray();
        var program = new Mock<IPgUpProgram>();
        var processor = CliProcessor
            .Setup(config => config
                .UseCommandsFrom(program.Object));
        foreach (var attribute in examples)
        {
            Debug.WriteLine(attribute.Example);
            Debug.WriteLine(attribute.Description);

            processor.Process($"pgup {attribute.Example}");
            Assert.True(program.Invocations.Count > 0, $"No method was called for example: {attribute.Example}");
            program.Invocations.Clear();
        }
    }
}
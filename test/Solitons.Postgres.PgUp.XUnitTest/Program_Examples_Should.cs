using Moq;
using Solitons.CommandLine.Common;

namespace Solitons.Postgres.PgUp;

// ReSharper disable once InconsistentNaming
public class Program_Examples_Should : CliCommandExamplesValidationTest
{
    private readonly Mock<IPgUpProgram> _programMock = new();

    [Fact]
    public void BeValid() => Validate();


    protected override void OnResult(string commandLine, int expectedInvocationsCount)
    {
        Assert.True(_programMock.Invocations.Count == 1, $"No method was called for the example: {commandLine}");
        _programMock.Invocations.Clear();
    }

    protected override object Program => _programMock.Object;
}
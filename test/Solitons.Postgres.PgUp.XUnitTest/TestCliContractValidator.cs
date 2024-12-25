using Solitons.CommandLine.Common;
using Solitons.Postgres.PgUp.CommandLine;

namespace Solitons.Postgres.PgUp;

public sealed class TestCliContractValidator : CliContractValidator<IProgram>
{
    [Fact]
    public void Run()
    {
        Validate(testCase =>
        {
            Assert.Fail($"Not invoked: {testCase.Example}");
        });
    }
}
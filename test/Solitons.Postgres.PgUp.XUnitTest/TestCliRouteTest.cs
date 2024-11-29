using Solitons.CommandLine.Common;

namespace Solitons.Postgres.PgUp;

public sealed class TestCliRouteTest : CliRouteTest<IPgUpProgram>
{
    [Fact]
    public void Run()
    {
        TestExamples(testCase =>
        {
            Assert.Fail($"Not invoked: {testCase.Example}");
        });
    }
}
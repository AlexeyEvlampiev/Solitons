using Solitons.CommandLine.Common;
using Solitons.CommandLine.Reflection;
using Solitons.Postgres.PgUp.CommandLine;

namespace Solitons.Postgres.PgUp;

// ReSharper disable once InconsistentNaming
public class Program_Examples_Should : CliContractValidator<IPgUpCommandLineContract>
{

    [Fact]
    public void ImplementPgUpCliContract()
    {
        Validate(OnFailure);
    }

    private void OnFailure(CliCommandExampleAttribute example)
    {
        Assert.Fail($"{example.Example} did not trigger the dedicated cli action.");
    }
}
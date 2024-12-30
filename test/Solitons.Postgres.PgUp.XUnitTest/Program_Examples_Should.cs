using Solitons.CommandLine.Common;
using Solitons.CommandLine.Reflection;

namespace Solitons.Postgres.PgUp;

// ReSharper disable once InconsistentNaming
public class Program_Examples_Should : CliContractValidator<IPgUp>
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
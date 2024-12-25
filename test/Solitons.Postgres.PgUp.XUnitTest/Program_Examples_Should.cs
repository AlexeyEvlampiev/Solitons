using Solitons.CommandLine.Common;
using Solitons.CommandLine.Reflection;
using Solitons.Postgres.PgUp.CommandLine;




// ReSharper disable once InconsistentNaming
public class Program_Examples_Should : CliContractValidator<IPgUpCommandLineContract>
{

    [Fact]
    public void ImplementPgUpCliContract()
    {
        Validate(OnFailure);
    }

    private void OnFailure(CliCommandExampleAttribute failedInvocation)
    {
        throw new NotImplementedException();
    }
}
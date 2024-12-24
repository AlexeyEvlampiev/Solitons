using System.Diagnostics;
using Solitons.CommandLine;
using Solitons.CommandLine.Common;



// ReSharper disable once InconsistentNaming
public class Program_Examples_Should : CliContractValidator<Solitons.Postgres.PgUp.IProgram>
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
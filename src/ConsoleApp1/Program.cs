

using Solitons.CommandLine;
using Solitons.CommandLine.Mercury;
using Solitons.Postgres.PgUp;

namespace ConsoleApp1;

public  sealed class Program : Solitons.CommandLine.Common.CliContractValidator<IProgram>
{
    public static int Main(params string[] args)
    {
        var cl = CliCommandLine.FromArgs("prog --flag --value a --map.aaa bbb");

        var processor = CliProcessorVNext
            //.Process<Solitons.Postgres.PgUp.Program>("pgup init . ");
            .Process<Solitons.Postgres.PgUp.Program>(@"
            pgup deploy project.json 
            --host localhost
            --user alexey
            --password hello-world
            --parameters[dbname] mydb 
            --parameters.dbowner mydbadmin");

        //var program = new Program();
        //program.TestExamples(example => throw new InvalidOperationException($"Not invoked: {example.Example}"));
        return 0;
    }

     
}
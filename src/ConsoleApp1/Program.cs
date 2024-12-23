

using Solitons.CommandLine.Mercury;
using Solitons.Postgres.PgUp;

namespace ConsoleApp1;

public  sealed class Program : Solitons.CommandLine.Common.CliRouteTest<IPgUpProgram>
{
    public static int Main(params string[] args)
    {
        var processor = CliProcessorVNext
            //.Process<Solitons.Postgres.PgUp.Program>("pgup init . ");
            .Process<Solitons.Postgres.PgUp.Program>(@"
            pgup deploy project.json 
            --host localhost
            --user alexey
            --password hello-world
            --parameter[dbname] mydb 
            --parameter.dbowner mydbadmin");

        //var program = new Program();
        //program.TestExamples(example => throw new InvalidOperationException($"Not invoked: {example.Example}"));
        return 0;
    }

     
}
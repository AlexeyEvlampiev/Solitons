

using Solitons.CommandLine.Mercury;
using Solitons.Postgres.PgUp;

namespace ConsoleApp1;

public  sealed class Program : Solitons.CommandLine.Common.CliRouteTest<IPgUpProgram>
{
    public static int Main(params string[] args)
    {
        var processor = new CliProcessorVNext();
        processor.Process("pgup init . --parameters[dbname] mydb --parameters.dbowner mydbadmin --help");
        //var program = new Program();
        //program.TestExamples(example => throw new InvalidOperationException($"Not invoked: {example.Example}"));
        return 0;
    }

     
}


using Solitons.Postgres.PgUp;

namespace ConsoleApp1;

public  sealed class Program : Solitons.CommandLine.Common.CliRouteTest<IPgUpProgram>
{
    public static int Main(params string[] args)
    {
        var program = new Program();
        program.TestExamples(example => throw new InvalidOperationException($"Not invoked: {example.Example}"));
        return 0;
    }

    
}
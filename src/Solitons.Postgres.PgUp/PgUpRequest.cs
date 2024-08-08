using System.Data.Common;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Npgsql;
using Solitons.CommandLine;

namespace Solitons.Postgres.PgUp;

public abstract class PgUpRequest
{

    protected PgUpRequest(string connectionString)
    {
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            ConnectionString = builder.ToString();
        }
        catch (Exception e) when(e is ArgumentException || e is FormatException)
        {
            throw new CliExitException("Invalid connection string format");
        }
    }

    public string ConnectionString { get; }

    public async Task TestConnectionAsync(CancellationToken cancellation)
    {
        await Observable
            .Using(
                () => new NpgsqlConnection(ConnectionString), 
                connection => connection
                    .OpenAsync(cancellation)
                    .ToObservable())
            .WithRetryTrigger(trigger => trigger
                .Where(trigger.Exception is DbException{ IsTransient: true})
                .Where(trigger.AttemptNumber < 100)
                .Do(() => Console.WriteLine(trigger.Exception.Message))
                .Do(() => Trace.TraceError(trigger.Exception.ToString()))
                .Delay(TimeSpan
                    .FromMilliseconds(100)
                    .ScaleByFactor(2.0, trigger.AttemptNumber))
                .Do(() => Trace.TraceInformation($"Connection test retry. Attempt: {trigger.AttemptNumber}")))
            .Catch(Observable.Throw<Unit>(new CliExitException("Connection failed")));
    }

}
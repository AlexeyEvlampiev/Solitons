using System.Data.Common;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Npgsql;
using Solitons.CommandLine;

namespace Solitons.Postgres.PgUp;

public static class PgUpUtil
{
    public static async Task TestConnectionAsync(
        string connectionString,
        TimeSpan? timeout = default, 
        CancellationToken cancellation = default)
    {
        timeout ??= TimeSpan.FromSeconds(3);
        cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellation,
            new CancellationTokenSource(timeout.Value).Token).Token;
        await Observable
            .Using(
                () => new NpgsqlConnection(connectionString),
                connection => connection
                    .OpenAsync(cancellation)
                    .ToObservable())
            .WithRetryTrigger(trigger => trigger
                .Where(trigger.Exception is DbException { IsTransient: true })
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
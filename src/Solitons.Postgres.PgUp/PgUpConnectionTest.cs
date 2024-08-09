using Solitons.CommandLine;
using System.Data.Common;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive;
using System.Reactive.Threading.Tasks;
using Npgsql;

namespace Solitons.Postgres.PgUp;

internal class PgUpConnectionTest
{
    public static Task TestAsync(
        string connectionString,
        TimeSpan timeout)
    {
        var cancellation = new CancellationTokenSource(timeout).Token;
        
        return Observable
            .Using(
                BuildConnection,
                connection => connection.Open(cancellation))
            .Catch((ArgumentException e) => Exit(e))
            .Catch((FormatException e) => Exit(e))
            .WithRetryTrigger(trigger => trigger
                .Where(trigger.Exception is DbException { IsTransient: true })
                .Where(trigger.AttemptNumber < 100)
                .Do(() => Console.WriteLine(trigger.Exception.Message))
                .Do(() => Trace.TraceError(trigger.Exception.ToString()))
                .Delay(TimeSpan
                    .FromMilliseconds(100)
                    .ScaleByFactor(2.0, trigger.AttemptNumber))
                .Do(() => Trace.TraceInformation($"Connection test retry. Attempt: {trigger.AttemptNumber}")))
            .Catch(Observable.Throw<Unit>(new CliExitException("Connection failed")))
            .ToTask(cancellation);

        NpgsqlConnection BuildConnection()
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                ApplicationName = "PgUp",
                Timeout = Convert.ToInt32(timeout.TotalSeconds)
            };
            return new NpgsqlConnection(builder.ConnectionString);
        }

        IObservable<Unit> Exit(Exception e) => Observable
            .Throw<Unit>(new CliExitException("Invalid connection string"));
    }



}
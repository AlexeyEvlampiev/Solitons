using System.Data.Common;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Npgsql;
using Solitons.CommandLine;
using Solitons.Data;
using Solitons.Postgres.PgUp.Core.Formatting;

namespace Solitons.Postgres.PgUp.Core;

public sealed class PgUpSession(string databaseOwner, TimeSpan timeout) : IPgUpSession
{
    private readonly CancellationToken _cancellation = new CancellationTokenSource(timeout).Token;


    private async Task TestConnectionAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(_cancellation);
    }

    private async Task DropDatabaseIfExistsAsync(string connectionString, string databaseName)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        connection.Notice += (_, args) => Console.WriteLine(args.Notice.MessageText);
        var commandText = $"DROP DATABASE IF EXISTS {databaseName} WITH(FORCE);";

        await connection.OpenAsync(_cancellation);
        await connection.ExecuteNonQueryAsync(commandText, _cancellation);
    }

    private async Task CreateDatabaseIfNotExistsAsync(
        string connectionString,
        string databaseName,
        string databaseOwner)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        connection.Notice += (_, args) => Console.WriteLine(args.Notice.MessageText);
        await connection.OpenAsync(_cancellation);
        bool databaseExists = await connection.ExecuteScalarAsync<bool>(@$"
        DO $$
        BEGIN
            IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = '{databaseOwner}') THEN
                CREATE ROLE {databaseOwner} NOLOGIN;
            END IF;
        END $$;
        GRANT {databaseOwner} TO CURRENT_USER;
        SELECT EXISTS(SELECT 1 FROM pg_database WHERE datname = '{databaseName}');
        ", _cancellation);
        if (databaseExists)
        {
            await connection.ExecuteNonQueryAsync(
                $"ALTER DATABASE {databaseName} OWNER TO {databaseOwner};",
                _cancellation);
        }
        else
        {
            await connection.ExecuteNonQueryAsync(
                $"CREATE DATABASE {databaseName} OWNER {databaseOwner};",
                _cancellation);
        }
    }

    private async Task ExecTransaction(
        PgUpTransaction pgUpTransaction,
        string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        connection.Notice += (_, args) => Console.WriteLine(args.Notice.MessageText);
        await connection.OpenAsync(_cancellation);
        await using var transaction = await connection.BeginTransactionAsync(_cancellation);
        using var hasher = new SqlCommandTextHasher();

        foreach (var batch in pgUpTransaction.GetBatches())
        {
            var builder = new PgUpCommandBuilder(batch.CustomExecCommand);
            foreach (var script in batch.GetScripts())
            {
                Console.WriteLine(PgUpScriptDisplayRtt.Build(script.RelativePath));
                if (hasher.IsEmpty(script.Content))
                {
                    Console.WriteLine(@"The SQL script contains only whitespace, comments, or empty content. Skipping processing.");
                    continue;
                }

                await connection.ExecuteNonQueryAsync($"SET ROLE {databaseOwner};", _cancellation);
                await using var command = builder.Build(script.RelativePath, script.Content, script.Checksum, connection);
                await command.ExecuteNonQueryAsync(_cancellation);
            }
        }

        await transaction.CommitAsync(_cancellation);
    }


    [DebuggerStepThrough]
    Task IPgUpSession.TestConnectionAsync(string connectionString)
    {
        _cancellation.ThrowIfCancellationRequested();
        return Observable
            .FromAsync(() => TestConnectionAsync(connectionString))
            .Catch((ArgumentException e) => PgUpExitException.InvalidConnectionString(e).AsObservable<Unit>())
            .Catch((FormatException e) => PgUpExitException.InvalidConnectionString(e).AsObservable<Unit>())
            .WithRetryTrigger(trigger => trigger
                .Where(trigger.Exception is DbException { IsTransient: true })
                .Where(trigger.ElapsedTimeSinceFirstException < (timeout * 0.01).Min(TimeSpan.FromSeconds(5)))
                .Do(() => Console.WriteLine(trigger.Exception.Message))
                .Do(() => Trace.TraceError(trigger.Exception.ToString()))
                .Delay(TimeSpan
                    .FromMilliseconds(100)
                    .ScaleByFactor(2.0, trigger.AttemptNumber))
                .Do(() => Trace.TraceInformation($"Connection test retry. Attempt: {trigger.AttemptNumber}")))
            .Catch(CliExitException.AsObservable<Unit>("Connection failed"))
            .ToTask(_cancellation.JoinTimeout(timeout));

    }

    [DebuggerStepThrough]
    Task IPgUpSession.DropDatabaseIfExistsAsync(string connectionString, string databaseName)
    {
        _cancellation.ThrowIfCancellationRequested();
        return Observable
            .FromAsync(() => DropDatabaseIfExistsAsync(connectionString, databaseName))
            .WithRetryTrigger(trigger => trigger
                .Where(trigger.Exception is DbException { IsTransient: true })
                .Where(trigger.AttemptNumber <= 5)
                .Do(() => Console.WriteLine(trigger.Exception.Message))
                .Do(() => Trace.TraceError(trigger.Exception.ToString()))
                .Delay(TimeSpan
                    .FromMilliseconds(100)
                    .ScaleByFactor(2.0, trigger.AttemptNumber))
                .Do(() => Trace.TraceInformation($"Connection test retry. Attempt: {trigger.AttemptNumber}")))
            .ToTask(_cancellation.JoinTimeout(timeout * 0.1));
    }

    [DebuggerStepThrough]
    Task IPgUpSession.ProvisionDatabaseAsync(
        string connectionString,
        string databaseName,
        string databaseOwner)
    {
        _cancellation.ThrowIfCancellationRequested();
        return Observable
            .FromAsync(() => CreateDatabaseIfNotExistsAsync(connectionString, databaseName, databaseOwner))
            .WithRetryTrigger(trigger => trigger
                .Where(_ => trigger.Exception is DbException { IsTransient: true })
                .Do(_ => Console.WriteLine(trigger.Exception.Message))
                .Delay(_ => TimeSpan
                    .FromMicroseconds(100)
                    .ScaleByFactor(2, trigger.AttemptNumber)
                    .Max(TimeSpan.FromSeconds(30))))
            .Catch(CliExitException.AsObservable<Unit>(
                $"Failed to create database {databaseName}"))
            .ToTask(_cancellation.JoinTimeout(timeout * 0.1));
    }

    [DebuggerStepThrough]
    Task IPgUpSession.ExecuteAsync(
        PgUpTransaction pgUpTransaction,
        string connectionString)
    {
        return Observable
            .FromAsync(() => ExecTransaction(pgUpTransaction, connectionString))
            .WithRetryTrigger((trigger) => trigger
                .Where(_ => trigger.Exception is DbException { IsTransient: true })
                .Do(_ => Trace.TraceWarning($"Command failed on attempt {trigger.AttemptNumber}"))
                .Delay(TimeSpan
                    .FromMilliseconds(200)
                    .ScaleByFactor(1.1, trigger.AttemptNumber)))
            .ToTask(_cancellation);
    }
}
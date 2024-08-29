using System.Diagnostics;
using Azure.Storage.Blobs;
using Parquet.Schema;
using Parquet;
using Parquet.Data;
using Solitons.Diagnostics;

namespace Solitons.AzProvider.Diagnostics;

public sealed class ParquetAsyncLogger : BufferedAsyncLogger
{
    private readonly Func<Task<Stream>> _streamFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParquetAsyncLogger"/> class.
    /// </summary>
    /// <param name="streamFactory">A factory function that returns a stream for storing log outputs.</param>
    /// <param name="config">The configuration options for buffering log messages.</param>
    [DebuggerStepThrough]
    private ParquetAsyncLogger(Func<Task<Stream>> streamFactory, Action<Options> config)
        : base(config)
    {
        // Use ThrowIf for argument validation
        ThrowIf.ArgumentNull(streamFactory, nameof(streamFactory));
        ThrowIf.ArgumentNull(config, nameof(config));

        _streamFactory = streamFactory;
    }

    /// <summary>
    /// Creates an instance of <see cref="ParquetAsyncLogger"/> that logs to an Azure Blob container.
    /// </summary>
    /// <param name="container">The Blob container client.</param>
    /// <param name="config">The configuration options for buffering log messages.</param>
    /// <returns>A new instance of <see cref="IAsyncLogger"/>.</returns>
    public static IAsyncLogger Create(
        BlobContainerClient container, 
        Action<Options> config)
    {
        return new ParquetAsyncLogger(OpenStreamAsync, config);
        Task<Stream> OpenStreamAsync()
        {
            var blob = container.GetBlobClient($"{Guid.NewGuid():N}.parquet");
            return blob.OpenWriteAsync(false);
        }
    }

    /// <summary>
    /// Asynchronously processes and writes a batch of buffered log messages to a Parquet format using the provided stream.
    /// </summary>
    /// <param name="args">The list of buffered <see cref="LogEventArgs"/> to process.</param>
    /// <returns>A task that represents the asynchronous logging operation.</returns>
    protected override async Task LogAsync(IList<LogEventArgs> args)
    {
        ThrowIf.ArgumentNull(args, nameof(args));
        if (args.Count == 0) return;

        try
        {
            var timestamp = DateTime.UtcNow;
            var schema = new ParquetSchema(
                new DataField<DateTime>("Timestamp"),
                new DataField<string>("LogJson"));
            var column1 = new DataColumn(
                schema.DataFields[0],
                args.Select(_ => timestamp).ToArray());

            var column2 = new DataColumn(
                schema.DataFields[1],
                args.Select(item => item.Content).ToArray());

            await using var stream = await _streamFactory().ConfigureAwait(false);
            using var writer = await ParquetWriter.CreateAsync(schema, stream);
            using var groupWriter = writer.CreateRowGroup();
            await groupWriter.WriteColumnAsync(column1);
            await groupWriter.WriteColumnAsync(column2);
            await stream.FlushAsync();
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Failed to write logs to Parquet output: {ex.Message}");
        }
    }
}
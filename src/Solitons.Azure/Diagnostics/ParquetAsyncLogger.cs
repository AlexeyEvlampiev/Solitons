using System.Diagnostics;
using Azure.Storage.Blobs;
using Parquet;
using Parquet.Data;
using Parquet.Schema;
using Solitons.Diagnostics;

namespace Solitons.Azure.Diagnostics;

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
        ThrowIf.ArgumentNull(streamFactory);
        ThrowIf.ArgumentNull(config);

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
        ThrowIf.ArgumentNull(container);
        ThrowIf.ArgumentNull(config);
        if (false == container.Exists())
        {
            throw new ArgumentException("The specified Blob container does not exist.", nameof(container));
        }
        return new ParquetAsyncLogger(OpenStreamAsync, config);
        Task<Stream> OpenStreamAsync()
        {
            var blob = container.GetBlobClient($"{Guid.NewGuid():N}.parquet");
            return blob.OpenWriteAsync(false);
        }
    }

    /// <summary>
    /// Creates an instance of <see cref="ParquetAsyncLogger"/> that logs to a local directory.
    /// </summary>
    /// <param name="directory">The directory where Parquet files will be stored.</param>
    /// <param name="config">The configuration options for buffering log messages.</param>
    /// <returns>A new instance of <see cref="IAsyncLogger"/>.</returns>
    public static IAsyncLogger Create(
        DirectoryInfo directory,
        Action<Options> config)
    {
        ThrowIf.ArgumentNull(directory);
        ThrowIf.ArgumentNull(config);
        if (false == directory.Exists)
        {
            throw new ArgumentException("The specified directory does not exist.");
        }
        var root = directory.FullName;
        return new ParquetAsyncLogger(OpenStreamAsync, config);
        Task<Stream> OpenStreamAsync()
        {
            var path = Path.Combine(root, $"{Guid.NewGuid():N}.parquet");
            return Task.FromResult((Stream)File.OpenWrite(path));
        }
    }

    /// <summary>
    /// Asynchronously processes and writes a batch of buffered log messages to a Parquet format using the provided stream.
    /// </summary>
    /// <param name="args">The list of buffered <see cref="LogEventArgs"/> to process.</param>
    /// <returns>A task that represents the asynchronous logging operation.</returns>
    protected override async Task LogAsync(IList<LogEventArgs> args)
    {
        ThrowIf.ArgumentNull(args);
        if (args.Count == 0) return;

        try
        {
            var timestamp = DateTime.UtcNow;
            var schema = new ParquetSchema(
                new DataField<DateTime>("Timestamp"),
                new DataField<string>("Json"));
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
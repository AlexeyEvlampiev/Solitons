using System.Diagnostics;
using System.Text;
using Azure.Messaging.EventGrid;
using Azure;
using Solitons.Diagnostics;
using Azure.Core;
using Azure.Messaging;

namespace Solitons.AzProvider.Diagnostics;

/// <summary>
/// A logger implementation that sends log messages to Azure Event Grid asynchronously.
/// </summary>
public sealed class EventGridAsyncLogger : BufferedAsyncLogger
{
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    private readonly EventGridPublisherClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventGridAsyncLogger"/> class.
    /// </summary>
    /// <param name="client">The <see cref="EventGridPublisherClient"/> used to send log events.</param>
    /// <param name="config">The configuration options for buffering log messages.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/> or <paramref name="config"/> is null.</exception>
    [DebuggerStepThrough]
    private EventGridAsyncLogger(EventGridPublisherClient client, Action<Options> config) 
        : base(config)
    {
        _client = client;
    }

    /// <summary>
    /// Creates an <see cref="EventGridAsyncLogger"/> with an <see cref="AzureKeyCredential"/>.
    /// </summary>
    /// <param name="topicEndpoint">The Event Grid topic endpoint.</param>
    /// <param name="azureKeyCredential">The Azure key credential for authentication.</param>
    /// <param name="config">The configuration options for buffering log messages.</param>
    /// <returns>A new instance of <see cref="IAsyncLogger"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="topicEndpoint"/> or <paramref name="azureKeyCredential"/> is null or empty.</exception>
    [DebuggerStepThrough]
    public static IAsyncLogger Create(
        string topicEndpoint,
        AzureKeyCredential azureKeyCredential, 
        Action<Options> config)
    {
        if (string.IsNullOrWhiteSpace(topicEndpoint))
            throw new ArgumentNullException(nameof(topicEndpoint));

        var client = new EventGridPublisherClient(new Uri(topicEndpoint), azureKeyCredential);
        return new EventGridAsyncLogger(client, config);
    }

    /// <summary>
    /// Creates an <see cref="EventGridAsyncLogger"/> with an <see cref="AzureSasCredential"/>.
    /// </summary>
    /// <param name="topicEndpoint">The Event Grid topic endpoint.</param>
    /// <param name="azureSasCredential">The Azure SAS credential for authentication.</param>
    /// <param name="config">The configuration options for buffering log messages.</param>
    /// <returns>A new instance of <see cref="IAsyncLogger"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="topicEndpoint"/> or <paramref name="azureSasCredential"/> is null or empty.</exception>
    [DebuggerStepThrough]
    public static IAsyncLogger Create(
        string topicEndpoint, 
        AzureSasCredential azureSasCredential, 
        Action<Options> config)
    {
        if (string.IsNullOrWhiteSpace(topicEndpoint))
            throw new ArgumentNullException(nameof(topicEndpoint));

        var client = new EventGridPublisherClient(new Uri(topicEndpoint), azureSasCredential);
        return new EventGridAsyncLogger(client, config);
    }

    /// <summary>
    /// Creates an <see cref="EventGridAsyncLogger"/> with a <see cref="TokenCredential"/>.
    /// </summary>
    /// <param name="topicEndpoint">The Event Grid topic endpoint.</param>
    /// <param name="tokenCredential">The Azure token credential for authentication.</param>
    /// <param name="config">The configuration options for buffering log messages.</param>
    /// <returns>A new instance of <see cref="IAsyncLogger"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="topicEndpoint"/> or <paramref name="tokenCredential"/> is null or empty.</exception>
    [DebuggerStepThrough]
    public static IAsyncLogger Create(
        string topicEndpoint, 
        TokenCredential tokenCredential,
        Action<Options> config)
    {
        if (string.IsNullOrWhiteSpace(topicEndpoint))
            throw new ArgumentNullException(nameof(topicEndpoint));

        var client = new EventGridPublisherClient(new Uri(topicEndpoint), tokenCredential);
        return new EventGridAsyncLogger(client, config);
    }



    /// <summary>
    /// Asynchronously processes and sends a batch of buffered log messages to Azure Event Grid.
    /// </summary>
    /// <param name="args">The list of buffered <see cref="LogEventArgs"/> to process.</param>
    /// <returns>A task that represents the asynchronous logging operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="args"/> is null.</exception>
    protected override async Task LogAsync(IList<LogEventArgs> args)
    {
        ThrowIf.ArgumentNull(args);
        if (args.Count == 0)
        {
            return;
        }
        var events = args.Select(arg => new CloudEvent(
            "/ap/logs",
            "log",
            new BinaryData(Encoding.UTF8.GetBytes(arg.Content)),
            "application/json"));

        try
        {
            // Send the pre-encoded CloudEvents directly to Event Grid
            await _client.SendEventsAsync(events);
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Failed to send log to Event Grid: {ex.Message}");
        }
    }
}
using System.Diagnostics;
using Azure.Messaging.EventGrid;
using Azure;
using Solitons.Diagnostics;
using Solitons.Diagnostics.Common;
using Azure.Core;

namespace Solitons.AzProvider.Diagnostics;

public sealed class EventGridAsyncLogger : AsyncLogger
{
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    private readonly EventGridPublisherClient _client;

    private EventGridAsyncLogger(EventGridPublisherClient client)
    {
        _client = client;
    }

    // Constructor using AzureKeyCredential (SAS Key)
    public static IAsyncLogger Create(string topicEndpoint, AzureKeyCredential azureKeyCredential)
    {
        if (string.IsNullOrWhiteSpace(topicEndpoint))
            throw new ArgumentNullException(nameof(topicEndpoint));

        var client = new EventGridPublisherClient(new Uri(topicEndpoint), azureKeyCredential);
        return new EventGridAsyncLogger(client);
    }

    // Constructor using AzureSasCredential (Shared Access Signature)
    public static IAsyncLogger Create(string topicEndpoint, AzureSasCredential azureSasCredential)
    {
        if (string.IsNullOrWhiteSpace(topicEndpoint))
            throw new ArgumentNullException(nameof(topicEndpoint));

        var client = new EventGridPublisherClient(new Uri(topicEndpoint), azureSasCredential);
        return new EventGridAsyncLogger(client);
    }

    // Constructor using TokenCredential (Managed Identity or OAuth)
    public static IAsyncLogger Create(string topicEndpoint, TokenCredential tokenCredential)
    {
        if (string.IsNullOrWhiteSpace(topicEndpoint))
            throw new ArgumentNullException(nameof(topicEndpoint));

        var client = new EventGridPublisherClient(new Uri(topicEndpoint), tokenCredential);
        return new EventGridAsyncLogger(client);
    }

    protected override async Task LogAsync(LogEventArgs args)
    {
        if (args == null)
        {
            throw new ArgumentNullException(nameof(args));
        }

        string content = args.Content;

        // Create the event to send to Azure Event Grid
        var eventGridEvent = new EventGridEvent(
            subject: $"Log: {args.Level}",
            eventType: "Solitons.Diagnostics.Log",
            dataVersion: "1.0",
            data: content
        );

        try
        {
            // Send the event to Azure Event Grid
            await _client.SendEventAsync(eventGridEvent);
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Failed to send log to Event Grid: {ex.Message}");
        }
    }
}
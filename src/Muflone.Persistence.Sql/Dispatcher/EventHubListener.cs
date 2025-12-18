using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Muflone.Persistence.Sql.Helpers;
using Muflone.Persistence.Sql.Models;
using System.Text.Json;

namespace Muflone.Persistence.Sql.Dispatcher;

public sealed class EventHubListener(
    EventHubParameters eventHubParameters,
    IEventBus eventBus,
    ILogger<EventHubListener> logger)
    : IAsyncDisposable
{
    private readonly EventProcessorClient _eventProcessorClient = new(
        new BlobContainerClient(
            eventHubParameters.BlobStorageConnectionString,
            eventHubParameters.BlobStorageContainerName),
        "eventstore",
        eventHubParameters.EventHubConnectionString,
        eventHubParameters.EventHubName);
    
    private CancellationTokenSource? _cts;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        // Register handlers for processing events and errors
        _eventProcessorClient.ProcessEventAsync += ProcessEventHandler;
        _eventProcessorClient.ProcessErrorAsync += ProcessErrorHandler;
        
        logger.LogInformation("Starting Event Hub processor");
        
        await _eventProcessorClient.StartProcessingAsync(cancellationToken);
    }
    
    private async Task ProcessEventHandler(ProcessEventArgs eventArgs)
    {
        try
        {
            // Deserialize the event data
            using var doc = JsonDocument.Parse(eventArgs.Data.Body.ToArray());
            var root = doc.RootElement;
            var dataJson = root.GetProperty("data");
 
            using var innerDoc = JsonDocument.Parse(dataJson.GetString()!);
            var data = innerDoc.RootElement;
            var cols = data.GetProperty("eventsource").GetProperty("cols").EnumerateArray();
            var current = JsonSerializer.Deserialize<Dictionary<string, string>>(data.GetProperty("eventrow").GetProperty("current").GetString()!);
 
            var @event = RepositoryHelper.DeserializeCloudEvent(GetEventElements(cols, current!));
            await eventBus.PublishAsync(@event, _cts!.Token).ConfigureAwait(false);
 
            // Persist progress so we don't reprocess this event on restart
            await eventArgs.UpdateCheckpointAsync();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex.Message);
            Console.ResetColor();
        }
    }
    
    private static ResolvedCloudEvent GetEventElements(JsonElement.ArrayEnumerator cols, Dictionary<string, string> current)
    {
        string metadata = string.Empty;
        string data = string.Empty;
        
        foreach (var name in cols.Select(col => col.GetProperty("name").GetString()))
        {
            switch (name)
            {
                case "Metadata":
                    metadata = current[name];
                    break;
                case "Data":
                    data = current[name];
                    break;
            }
        }
 
        return new ResolvedCloudEvent(metadata, data);
    }

    private static Task ProcessErrorHandler(ProcessErrorEventArgs e)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(e.Exception.Message);
        Console.ResetColor();
        return Task.CompletedTask;
    }

    #region Dispose
    public async ValueTask DisposeAsync()
    {
        await _cts?.CancelAsync()!;
        await _eventProcessorClient.StopProcessingAsync();
        // ReSharper disable once GCSuppressFinalizeForTypeWithoutDestructor
        GC.SuppressFinalize(this);
    }
    #endregion
}
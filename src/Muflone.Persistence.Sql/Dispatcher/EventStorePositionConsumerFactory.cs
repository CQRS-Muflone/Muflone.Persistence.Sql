using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Storage.Blobs;

namespace Muflone.Persistence.Sql.Dispatcher;

public static class EventStorePositionConsumerFactory
{
    public static EventProcessorClient Build(EventHubOptions eventHubOptions)
    {   
        var storageClient = new BlobContainerClient(eventHubOptions.BlobStorageConnectionString, 
            eventHubOptions.BlobStorageContainerName);
        
        var eventProcessorClient = new EventProcessorClient(
            storageClient,
            EventHubConsumerClient.DefaultConsumerGroupName,
            eventHubOptions.ConnectionString,
            eventHubOptions.EventHubName);
        
        return eventProcessorClient;
    }
}
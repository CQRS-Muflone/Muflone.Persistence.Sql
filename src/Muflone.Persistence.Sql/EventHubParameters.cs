namespace Muflone.Persistence.Sql;

public record EventHubParameters(string EventHubConnectionString, string EventHubName, 
    string BlobStorageConnectionString, string BlobStorageContainerName);
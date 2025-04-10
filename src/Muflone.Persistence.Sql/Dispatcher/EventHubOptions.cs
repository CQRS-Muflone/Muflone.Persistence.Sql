namespace Muflone.Persistence.Sql.Dispatcher;

public record EventHubOptions(
    string UserId,
    string ConnectionString,
    string EventHubName,
    string BlobStorageConnectionString,
    string BlobStorageContainerName);
 
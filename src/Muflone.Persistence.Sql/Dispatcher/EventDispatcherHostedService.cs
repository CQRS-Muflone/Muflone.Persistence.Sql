using Microsoft.Extensions.Hosting;

namespace Muflone.Persistence.Sql.Dispatcher;

public class EventDispatcherHostedService(
    EventDispatcher eventDispatcher) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await eventDispatcher.RunAsync(stoppingToken);
    }
}
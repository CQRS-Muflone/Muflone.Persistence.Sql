using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Muflone.Persistence.Sql.Dispatcher;

public class EventDispatcherHostedService(
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var eventDispatcher = scope.ServiceProvider.GetRequiredService<EventDispatcher>();
        await eventDispatcher.RunAsync(stoppingToken);
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Muflone.Messages.Events;
using Muflone.Persistence.Sql.Helpers;
using Muflone.Persistence.Sql.Models;

namespace Muflone.Persistence.Sql.Dispatcher;

public sealed class EventDispatcher(EventStoreContext eventStoreContext,
    IEventBus eventBus,
    ILoggerFactory loggerFactory) : IEventDispatcher
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<EventDispatcher>();
    
    public async Task RunAsync(CancellationToken stoppingToken)
    {
        await DispatchAllEventsAsync(0, stoppingToken);
    }
    
    public async Task DispatchAllEventsAsync(long lastPosition, CancellationToken cancellationToken = new ())
    {
        try
        {
            var readResult = await eventStoreContext.Set<EventStore>()
                .ToListAsync(cancellationToken: cancellationToken);

            foreach (var @event in readResult)
                await eventBus
                    .PublishAsync((DomainEvent) RepositoryHelper.DeserializeEvent(new ResolvedEvent(@event.Metadata, @event.Data)),
                        cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during DispatchAllEventsAsync");
        }
    }
}
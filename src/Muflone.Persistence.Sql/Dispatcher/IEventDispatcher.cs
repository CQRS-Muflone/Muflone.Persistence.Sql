namespace Muflone.Persistence.Sql.Dispatcher;

public interface IEventDispatcher
{
    Task DispatchAllEventsAsync(long lastPosition, CancellationToken cancellationToken = new ());
}
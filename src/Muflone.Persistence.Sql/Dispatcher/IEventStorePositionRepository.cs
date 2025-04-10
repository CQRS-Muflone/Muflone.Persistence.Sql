using Muflone.Persistence.Sql.Persistence;

namespace Muflone.Persistence.Sql.Dispatcher;

public interface IEventStorePositionRepository
{
    Task<IEventStorePosition> GetLastPositionAsync();
    Task SaveAsync(IEventStorePosition position);
}
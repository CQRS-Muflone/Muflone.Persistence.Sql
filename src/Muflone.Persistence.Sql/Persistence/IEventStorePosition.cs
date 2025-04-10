namespace Muflone.Persistence.Sql.Persistence;

public interface IEventStorePosition
{
    long CommitPosition { get; }
    long PreparePosition { get; }
}
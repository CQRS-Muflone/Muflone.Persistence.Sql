namespace Muflone.Persistence.Sql.Persistence;

public sealed class EventStorePosition
    : IEventStorePosition
{
    public long CommitPosition { get; private set; }
    public long PreparePosition { get; private set; }
    
    protected EventStorePosition()
    {}
    
    public static EventStorePosition Create(long commitPosition, long preparePosition)
    {
        return new EventStorePosition(commitPosition, preparePosition);
    }

    private EventStorePosition(long commitPosition, long preparePosition)
    {
        CommitPosition = commitPosition;
        PreparePosition = preparePosition;
    }
}
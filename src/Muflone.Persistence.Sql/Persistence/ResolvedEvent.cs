namespace Muflone.Persistence.Sql.Persistence;

public readonly struct ResolvedEvent
{
    public readonly EventRecord Event;
    public readonly long? OriginalPosition;
    
    public ResolvedEvent(EventRecord @event, EventRecord? link, long? commitPosition)
    {
        Event = @event;
        OriginalPosition = commitPosition ?? 0;
    }
}
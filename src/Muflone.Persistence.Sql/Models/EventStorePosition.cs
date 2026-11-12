namespace Muflone.Persistence.Sql.Models;

public class EventStorePosition
{
    public int Id { get; private set; }
    public long CommitPosition { get; private set;  }
    public long PreparePosition { get; private set;  }
    
    protected EventStorePosition()
    {}

    public static EventStorePosition Create(long commitPosition) => new(commitPosition);

    private EventStorePosition(long commitPosition)
    {
        CommitPosition = commitPosition;
        PreparePosition = commitPosition;
    }
    
    public void UpdateCommitPosition(long commitPosition)
    {
        CommitPosition = commitPosition;
        PreparePosition = commitPosition;
    }
}
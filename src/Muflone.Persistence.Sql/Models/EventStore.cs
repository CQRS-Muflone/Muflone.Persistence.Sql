using System.Text;

namespace Muflone.Persistence.Sql.Models;

public class EventStore
{
    public string MessageId { get; private set; } = string.Empty;
    public string AggregateId { get; private set; } = string.Empty;
    public string AggregateName { get; private set; } = string.Empty;
    public string AggregateType { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public byte[] Data { get; private set; } = [];
    public byte[] Metadata { get; private set; } = [];
    public int Version { get; private set;  }
    public long CommitPosition { get; private set;  }
    
    protected EventStore()
    {}
    
    internal static EventStore Create(string messageId, string aggregateId, string aggregateName,
        string aggregateType, string eventType, string data, string metadata, int version)
    {
        return new EventStore(messageId, aggregateId, aggregateName, aggregateType, eventType, data, metadata,
            version);
    }

    private EventStore(string messageId, string aggregateId, string aggregateName, string aggregateType,
        string eventType, string data, string metadata, int version)
    {
        MessageId = messageId;
        AggregateId = aggregateId;
        AggregateName = aggregateName;
        AggregateType = aggregateType;
        EventType = eventType;
        Data = Encoding.UTF8.GetBytes(data);
        Metadata = Encoding.UTF8.GetBytes(metadata);
        Version = version;
    }
}
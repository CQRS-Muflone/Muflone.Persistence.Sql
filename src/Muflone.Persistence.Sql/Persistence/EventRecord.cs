namespace Muflone.Persistence.Sql.Persistence;

public class EventRecord
{
    public string MessageId { get; private set; }
    public string AggregateId { get; private set; }
    public string AggregateName { get; private set; }
    public string AggregateType { get; private set; }
    public string EventType { get; private set; }
    public byte[] Data { get; private set; }
    public byte[] Metadata { get; private set; }
    public long Version { get; private set; }
    public long CommitPosition { get; private set; }
    
    protected EventRecord()
    {}

    public static EventRecord Create(Guid messageId, string aggregateId, string aggregateName, string aggregateType,
        string eventType, byte[] data, byte[] metadata, long version)
    {
        return new EventRecord(messageId, aggregateId, aggregateName, aggregateType, eventType, data, metadata, version);
    }

    private EventRecord(Guid messageId, string aggregateId, string aggregateName, string aggregateType,
        string eventType, byte[] data, byte[] metadata, long version)
    {
        MessageId = messageId.ToString();
        AggregateId = aggregateId;
        AggregateName = aggregateName;
        AggregateType = aggregateType;
        Data = data;
        EventType = eventType;
        Metadata = metadata;
        Version = version;
    }
}
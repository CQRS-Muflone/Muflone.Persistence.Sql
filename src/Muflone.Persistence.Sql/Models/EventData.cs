namespace Muflone.Persistence.Sql.Models;

public sealed class EventData
{
	public readonly Guid EventId;
	public readonly string Type;
	public readonly bool IsJson;
	public readonly string Data;
	public readonly string Metadata;

	/// <summary>
	/// Constructs a new EventData
	/// </summary>
	/// <param name="eventId">The ID of the event, used as part of the idempotent write check.</param>
	/// <param name="type">The name of the event type. It is strongly recommended that these
	/// use lowerCamelCase if projections are to be used.</param>
	/// <param name="isJson">Flag indicating whether the data and metadata are JSON.</param>
	/// <param name="data">The raw bytes of the event data.</param>
	/// <param name="metadata">The raw bytes of the event metadata.</param>
	public EventData(Guid eventId, string type, bool isJson, string data, string metadata)
	{
		EventId = eventId;
		Type = type;
		IsJson = isJson;
		Data = data ?? string.Empty;
		Metadata = metadata ?? string.Empty;
	}
}
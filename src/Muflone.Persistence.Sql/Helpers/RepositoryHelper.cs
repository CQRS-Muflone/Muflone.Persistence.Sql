using Muflone.Messages.Events;
using Muflone.Persistence.Sql.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace Muflone.Persistence.Sql.Helpers;

public static class RepositoryHelper
{
    private const string EventClrTypeHeader = "EventClrTypeName";
    
    public static EventData ToEventData(Guid eventId, object @event, IDictionary<string, object> headers)
    {
        var data = JsonConvert.SerializeObject(@event);
        var eventHeaders = new Dictionary<string, object>(headers) { { EventClrTypeHeader, @event.GetType().AssemblyQualifiedName! } };
        var metadata = JsonConvert.SerializeObject(eventHeaders);
        var typeName = @event.GetType().Name;
		
        return new EventData(eventId, typeName, true, data, metadata);
    }
    
    public static object DeserializeEvent(ResolvedEvent resolvedEvent)
    {
        try
        {
            var eventClrTypeName = JObject.Parse(Encoding.UTF8.GetString(resolvedEvent.Metadata.ToArray())).Property(EventClrTypeHeader)!.Value;
            return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(resolvedEvent.Data.ToArray()), Type.GetType(((string)eventClrTypeName)!)!)!;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    public static DomainEvent DeserializeCloudEvent(ResolvedCloudEvent cloudEvent)
    {
        try
        {
            var metadataBytes = Enumerable.Range(0, cloudEvent.CloudEventMetadata.Length / 2)
                .Select(x => Convert.ToByte(cloudEvent.CloudEventMetadata.Substring(x * 2, 2), 16))
                .ToArray();
            
            string json = Encoding.UTF8.GetString(metadataBytes);
            var eventHeaders = JsonConvert.DeserializeObject<Dictionary<string, object>>(json)!;
            
            var eventClrTypeName = eventHeaders[EventClrTypeHeader].ToString();
            var eventType = Type.GetType(eventClrTypeName!, throwOnError: true);
            
            var dataBytes = Enumerable.Range(0, cloudEvent.CloudEventData.Length / 2)
                .Select(x => Convert.ToByte(cloudEvent.CloudEventData.Substring(x * 2, 2), 16))
                .ToArray();
            var dataJson = Encoding.UTF8.GetString(dataBytes);

            return (DomainEvent) JsonConvert.DeserializeObject(dataJson, eventType!)!;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}
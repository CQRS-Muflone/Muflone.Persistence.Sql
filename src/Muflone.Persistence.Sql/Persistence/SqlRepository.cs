using System.Reflection;
using System.Text;
using Muflone.Core;
using Muflone.Persistence.Sql.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using AggregateNotFoundException = Muflone.Core.AggregateNotFoundException;

namespace Muflone.Persistence.Sql.Persistence;

public sealed class SqlRepository(SqlOptions sqlOptions) : IRepository
{
    private const string EventClrTypeHeader = "EventClrTypeName";
    private const string AggregateClrTypeHeader = "AggregateClrTypeName";
    private const string CommitIdHeader = "CommitId";
    private const string CommitDateHeader = "CommitDate";
    
    private readonly Func<Type, IDomainId, string> aggregateIdToStreamName;
    private static readonly JsonSerializerSettings SerializerSettings;

    static SqlRepository()
    {
        SerializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
            ContractResolver = new PrivateContractResolver()
        };
    }
    
    public Task<TAggregate?> GetByIdAsync<TAggregate>(IDomainId id, CancellationToken cancellationToken = new()) where TAggregate : class, IAggregate
    {
        return GetByIdAsync<TAggregate>(id, int.MaxValue, cancellationToken);
    }

    public async Task<TAggregate?> GetByIdAsync<TAggregate>(IDomainId id, long version,
        CancellationToken cancellationToken = new()) where TAggregate : class, IAggregate
    {
        if (version <= 0)
            throw new InvalidOperationException("Cannot get version <= 0");

        var aggregate = ConstructAggregate<TAggregate>();
        
        try
        {
            await using var facade = new EventStoreFacade(sqlOptions.ConnectionString);
            var readResult = facade.EventStore.Where(
                e => e.AggregateId.Equals(id.Value))
                .ToList();
            
            if (!readResult.Any())
                throw new AggregateNotFoundException(id, typeof(TAggregate));

            foreach (var @event in readResult)
                aggregate!.ApplyEvent(DeserializeEvent(@event));
        }
        catch (Exception e)
        {
            throw new AggregateNotFoundException(id, typeof(TAggregate));
        }

        return aggregate;
    }

    public async Task SaveAsync(IAggregate aggregate, Guid commitId, Action<IDictionary<string, object>> updateHeaders,
        CancellationToken cancellationToken = new())
    {
        var commitHeaders = new Dictionary<string, object>
        {
            { CommitIdHeader, commitId },
            { CommitDateHeader, DateTime.UtcNow},
            { AggregateClrTypeHeader, aggregate.GetType().AssemblyQualifiedName! }
        };
        updateHeaders(commitHeaders);

        var newEvents = aggregate.GetUncommittedEvents().Cast<object>().ToList();
        var eventsToSave = newEvents.Select(e => ToEventData(commitId, aggregate, e, commitHeaders)).ToList();

        try
        {
            await using var facade = new EventStoreFacade(sqlOptions.ConnectionString);
            foreach (var @event in eventsToSave)
            {
                facade.EventStore.Add(@event);
                await facade.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception e)
        {
            throw new AggregateSaveException(aggregate.Id, aggregate.GetType());
        }
        
        aggregate.ClearUncommittedEvents();
    }

    public async Task SaveAsync(IAggregate aggregate, Guid commitId, CancellationToken cancellationToken = new())
    {
        await SaveAsync(aggregate, commitId, headers => { }, cancellationToken);
    }
    
    private static TAggregate? ConstructAggregate<TAggregate>()
    {
        return (TAggregate)Activator.CreateInstance(typeof(TAggregate), true)!;
    }
    
    private static EventRecord ToEventData(Guid eventId, IAggregate aggregate, object @event, IDictionary<string, object> headers)
    {
        var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event, SerializerSettings));
        var eventHeaders = new Dictionary<string, object>(headers) { { EventClrTypeHeader, @event.GetType().AssemblyQualifiedName! } };
        var metadata = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(eventHeaders, SerializerSettings));
        var typeName = @event.GetType().Name;

        return EventRecord.Create(eventId, aggregate.Id.Value, aggregate.GetType().Name, aggregate.GetType().FullName!,
            typeName, data, metadata, aggregate.Version + 1);
    }
    
    private static object DeserializeEvent(EventRecord resolvedEvent)
    {
        var eventClrTypeName = JObject.Parse(Encoding.UTF8.GetString(resolvedEvent.Metadata.ToArray())).Property(EventClrTypeHeader)!.Value;
        return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(resolvedEvent.Data.ToArray()), Type.GetType(((string)eventClrTypeName)!)!)!;        
    }
    
    #region IDisposable Support
    private bool _disposedValue; // To detect redundant calls

    private void Dispose(bool disposing)
    {
        if (_disposedValue)
            return;
        
        if (disposing)
        {
            // TODO: dispose managed state (managed objects).
        }
        // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
        // TODO: set large fields to null.
        _disposedValue = true;
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    // ~SqlRepository() {
    //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
    //   Dispose(false);
    // }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
        // TODO: uncomment the following line if the finalizer is overridden above.
        // GC.SuppressFinalize(this);
    }
    #endregion
}

internal class PrivateContractResolver : DefaultContractResolver
{
    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Select(p => base.CreateProperty(p, memberSerialization))
            .Union(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Select(f => base.CreateProperty(f, memberSerialization)))
            .ToList();
        props.ForEach(p => { p.Writable = true; p.Readable = true; });
        return props;
    }
}
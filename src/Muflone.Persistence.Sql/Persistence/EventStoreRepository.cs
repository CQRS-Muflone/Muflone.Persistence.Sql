using Microsoft.EntityFrameworkCore;
using Muflone.Core;
using Muflone.Persistence.Sql.Helpers;
using Muflone.Persistence.Sql.Models;

namespace Muflone.Persistence.Sql.Persistence;

public sealed class EventStoreRepository : IRepository
{
	private const string AggregateClrTypeHeader = "AggregateClrTypeName";
	private const string CommitIdHeader = "CommitId";
	private const string CommitDateHeader = "CommitDate";

	private readonly Func<Type, string, string> _aggregateIdToStreamName;

	// private readonly BlobServiceClient _blobServiceClient;
	private readonly EventStoreContext _eventStoreContext;

	//This rename is needed to be consistent with naming convention of EventStore Javascript
	public EventStoreRepository(EventStoreContext eventStoreContext)
		: this(eventStoreContext, (type, aggregateId) => $"{type.Name.ToLower()}{aggregateId.Replace("-","")}")
	{
	}

	public EventStoreRepository(EventStoreContext eventStoreContext, Func<Type, string, string> aggregateIdToStreamName)
	{
		_eventStoreContext = eventStoreContext;
		_aggregateIdToStreamName = aggregateIdToStreamName;
	}
	
	public Task<TAggregate?> GetByIdAsync<TAggregate>(IDomainId id, CancellationToken cancellationToken = new ()) where TAggregate : class, IAggregate
	{
		return GetByIdAsync<TAggregate>(id, int.MaxValue, cancellationToken);
	}

	public async Task<TAggregate?> GetByIdAsync<TAggregate>(IDomainId id, long version,
		CancellationToken cancellationToken = new ()) where TAggregate : class, IAggregate
	{
		cancellationToken.ThrowIfCancellationRequested();
		
		if (version <= 0)
			throw new InvalidOperationException("Cannot get version <= 0");

		var aggregate = ConstructAggregate<TAggregate>();

		var readResult = await _eventStoreContext.Set<EventStore>()
			.Where(a => a.AggregateId.Equals(id.Value))
			.ToListAsync(cancellationToken: cancellationToken);
		
		foreach (var @event in readResult)
			aggregate.ApplyEvent(RepositoryHelper.DeserializeEvent(new ResolvedEvent(@event.Metadata, @event.Data)));

		if (aggregate.Version != version && version < int.MaxValue)
			throw new AggregateVersionException(id, typeof(TAggregate), aggregate.Version, version);

		return aggregate;
	}
	
	public async Task SaveAsync(IAggregate aggregate, Guid commitId, Action<IDictionary<string, object>> updateHeaders,
		CancellationToken cancellationToken = new ())
	{
		cancellationToken.ThrowIfCancellationRequested();
		
		var commitHeaders = new Dictionary<string, object>
		{
			{ CommitIdHeader, commitId },
			{ CommitDateHeader, DateTime.UtcNow},
			{ AggregateClrTypeHeader, aggregate.GetType().AssemblyQualifiedName! }
		};
		updateHeaders(commitHeaders);

		//Create a unique name for the container
		// var containerName = _aggregateIdToStreamName(aggregate.GetType(), aggregate.Id.Value);
		var aggregateName = aggregate.GetType().Name.ToLower();
		var newEvents = aggregate.GetUncommittedEvents().Cast<object>().ToList();
		var originalVersion = aggregate.Version - newEvents.Count;
		var expectedVersion = originalVersion == 0 ? ExpectedVersion.NoStream : originalVersion - 1;
		var eventsToSave = newEvents.Select(e => RepositoryHelper.ToEventData(Guid.NewGuid(), e, commitHeaders)).ToList();

		try
		{
			await using var transaction = await _eventStoreContext.Database.BeginTransactionAsync(cancellationToken);
			var dbSet = _eventStoreContext.Set<EventStore>();

			foreach (var entity in eventsToSave.Select(eventData => EventStore.Create(Guid.NewGuid().ToString(), aggregate.Id.Value,
				         aggregateName, 
				         aggregate.GetType().AssemblyQualifiedName!,
				         eventData.Type,
				         eventData.Data,
				         eventData.Metadata,
				         ++originalVersion)))
			{
				await dbSet.AddAsync(entity, cancellationToken);
			}
			await _eventStoreContext.SaveChangesAsync(cancellationToken);
			await transaction.CommitAsync(cancellationToken);
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex);
		}

		aggregate.ClearUncommittedEvents();
	}

	public Task SaveAsync(IAggregate aggregate, Guid commitId, CancellationToken cancellationToken = new()) =>
		SaveAsync(aggregate, commitId, _ => { }, cancellationToken);

    public Task SaveAsync(IAggregate aggregate, Guid commitId) => SaveAsync(aggregate, commitId, _ => { });

    private static TAggregate ConstructAggregate<TAggregate>() =>
	    (TAggregate) Activator.CreateInstance(typeof(TAggregate), true)!;

	#region Dispose
	private bool _disposedValue; // To detect redundant calls

	private void Dispose(bool disposing)
	{
		if (_disposedValue) return;
		
		if (disposing)
		{
			// TODO: dispose managed state (managed objects).
		}
		// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
		// TODO: set large fields to null.
		_disposedValue = true;
	}

	// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
	// ~EventStoreRepository() {
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

    Task<TAggregate?> IRepository.GetByIdAsync<TAggregate>(IDomainId id, CancellationToken cancellationToken) where TAggregate : class
    {
        throw new NotImplementedException();
    }

    Task<TAggregate?> IRepository.GetByIdAsync<TAggregate>(IDomainId id, long version, CancellationToken cancellationToken) where TAggregate : class
    {
        throw new NotImplementedException();
    }
    #endregion
}
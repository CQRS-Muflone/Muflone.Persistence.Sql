using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Muflone.Core;

namespace Muflone.Persistence.Sql.Persistence;

public class EventStoreFacade(string connectionString) : DbContext
{
    public DbSet<EventRecord> EventStore { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(connectionString);
        
        // Logging configuration
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter((category, level) => level == LogLevel.Information)
                .AddConsole();
        });

        optionsBuilder.UseLoggerFactory(loggerFactory);
        optionsBuilder.EnableSensitiveDataLogging();
        
        base.OnConfiguring(optionsBuilder);
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfiguration(new EventStoreMapping());
    }
    
    public EventRecord[] GetAggregateStreamByIdAsync(IDomainId id,
        int version = 0,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var eventsQueryable = EventStore.OrderBy(e => e.Version).AsQueryable();
        
        return version > 0 
            ? eventsQueryable.Where(e => e.Version >= version
                                         && e.AggregateId.Equals(id.Value)).ToArray() 
            : eventsQueryable.Where(e => e.AggregateId.Equals(id.Value)).ToArray();
    }
    
    public EventRecord[] GetAggregateStreamByIdAsync(string id,
        int version = 0,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var eventsQueryable = EventStore.OrderBy(e => e.Version).AsQueryable();
        
        return version > 0 
            ? eventsQueryable.Where(e => e.Version >= version
                                         && e.AggregateId.Equals(id)).ToArray() 
            : eventsQueryable.Where(e => e.AggregateId.Equals(id)).ToArray();
    }

    public EventRecord? GetEventByMessageIdAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        return EventStore.FirstOrDefault(e => e.MessageId.Equals(id));
    }
}
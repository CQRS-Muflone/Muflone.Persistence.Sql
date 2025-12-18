using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Muflone.Persistence.Sql.Mappings;
using Muflone.Persistence.Sql.Models;

namespace Muflone.Persistence.Sql;

public class EventStoreContext(DbContextOptions<EventStoreContext> options) : DbContext(options)
{
    public DbSet<EventStore> EventStore { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Logging configuration
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter((_, level) => level == LogLevel.Information)
                .AddConsole();
        });

        optionsBuilder.UseLoggerFactory(loggerFactory);
#if DEBUG
        optionsBuilder.EnableSensitiveDataLogging();
#endif
        
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new EventStoreMapping());
    }
}
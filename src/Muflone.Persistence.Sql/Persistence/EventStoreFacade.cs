using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
}
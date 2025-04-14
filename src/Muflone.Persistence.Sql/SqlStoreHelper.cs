using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Muflone.Persistence.Sql.Dispatcher;
using Muflone.Persistence.Sql.Persistence;

namespace Muflone.Persistence.Sql;

public static class SqlStoreHelper
{
    public static IServiceCollection AddSqlStore(this IServiceCollection services, IConfiguration configuration)
    {
        var sqlOptions = configuration.GetSection("Muflone:SqlStore")
            .Get<SqlOptions>()!;
        services.AddSingleton(sqlOptions);
        var eventHubOptions = configuration.GetSection("Muflone:EventHub")
            .Get<EventHubOptions>()!;
        services.AddSingleton(eventHubOptions);
        
        var eventProcessorClient = EventStorePositionConsumerFactory.Build(eventHubOptions);
        services.AddSingleton(eventProcessorClient);
        
        services.AddScoped<IRepository, SqlRepository>();
        services.AddHostedService<EventDispatcher>();
        
        return services;
    }
}
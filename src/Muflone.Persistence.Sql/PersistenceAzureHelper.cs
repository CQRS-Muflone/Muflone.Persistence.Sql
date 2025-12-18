using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Muflone.Persistence.Sql.Dispatcher;
using Muflone.Persistence.Sql.Persistence;

namespace Muflone.Persistence.Sql;

public static class PersistenceAzureHelper
{
	public static IServiceCollection AddEventstoreAzurePersistence(this IServiceCollection services,
		IConfigurationManager configurationManager)
	{
		services.AddDbContext<EventStoreContext>(options =>
			options.UseSqlServer(configurationManager["Muflone:SqlStore:ConnectionString"]!));
		services.AddScoped<IRepository, EventStoreRepository>();
		
		var eventhubParameters = configurationManager.GetSection("Muflone:EventHub").Get<EventHubParameters>();
		services.AddSingleton<EventHubListener>(sp => 
			new EventHubListener(
				eventhubParameters!,
				sp.GetRequiredService<IEventBus>(),
				sp.GetRequiredService<ILogger<EventHubListener>>()));
		services.AddHostedService<EventHubListenerHostedService>();
		
		// services.AddHostedService<EventDispatcherHostedService>(sp =>
		// 	new EventDispatcherHostedService(
		// 		new EventDispatcher(
		// 			sp.GetRequiredService<EventStoreContext>(),
		// 			sp.GetRequiredService<IEventBus>(),
		// 			sp.GetRequiredService<ILoggerFactory>())));

		return services;
	}
}
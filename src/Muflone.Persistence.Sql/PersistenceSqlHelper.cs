using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Muflone.Persistence.Sql.Dispatcher;
using Muflone.Persistence.Sql.Persistence;

namespace Muflone.Persistence.Sql;

public static class PersistenceSqlHelper
{
	public static IServiceCollection AddEventstoreSqlPersistence(this IServiceCollection services,
		IConfigurationManager configurationManager, bool ensureSchema = false)
	{
		services.AddDbContext<EventStoreContext>(options =>
			options.UseSqlServer(configurationManager["Muflone:SqlStore:ConnectionString"]!));
		services.AddScoped<IRepository, EventStoreRepository>();

		// Opt-in: create the EventStore / EventStorePosition tables at startup if they are missing.
		// Off by default so teams that manage schema via their own migration/deployment pipeline
		// (or with least-privilege DB accounts) are not surprised by the library issuing DDL.
		// Registered before the dispatcher so the schema exists before events are dispatched.
		if (ensureSchema)
			services.AddHostedService<EventStoreSchemaInitializerHostedService>();

		// var eventhubParameters = configurationManager.GetSection("Muflone:EventHub").Get<EventHubParameters>();
		// services.AddSingleton<EventHubListener>(sp =>
		// 	new EventHubListener(
		// 		eventhubParameters!,
		// 		sp.GetRequiredService<IEventBus>(),
		// 		sp.GetRequiredService<ILogger<EventHubListener>>()));
		// services.AddHostedService<EventHubListenerHostedService>();

		services.AddScoped<EventDispatcher>();
		services.AddHostedService<EventDispatcherHostedService>();

		return services;
	}
}
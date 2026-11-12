using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Muflone.Persistence.Sql.Persistence;

/// <summary>
/// Ensures the EventStore schema exists at application startup. Implemented as a plain
/// <see cref="IHostedService"/> (not a <see cref="BackgroundService"/>) so the host awaits the
/// schema creation in <see cref="StartAsync"/> before starting later-registered hosted services,
/// such as the event dispatcher that reads <c>EventStorePosition</c>.
/// </summary>
internal sealed class EventStoreSchemaInitializerHostedService(IServiceScopeFactory scopeFactory) : IHostedService
{
	public async Task StartAsync(CancellationToken cancellationToken)
	{
		using var scope = scopeFactory.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<EventStoreContext>();
		await EventStoreSchema.EnsureCreatedAsync(context, cancellationToken);
	}

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

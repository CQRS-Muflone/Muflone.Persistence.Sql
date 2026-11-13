using Microsoft.EntityFrameworkCore;
using Muflone.Persistence.Sql.Models;

namespace Muflone.Persistence.Sql.Persistence;

/// <summary>
/// Maintains the single <see cref="EventStorePosition"/> row, which holds the CommitPosition
/// of the last event that has been published to the event bus.
/// </summary>
internal static class EventStorePositionStore
{
	public static async Task AdvanceAsync(EventStoreContext context, long commitPosition,
		CancellationToken cancellationToken)
	{
		var positionSet = context.Set<EventStorePosition>();
		var position = await positionSet.FirstOrDefaultAsync(cancellationToken);

		if (position is null)
			await positionSet.AddAsync(EventStorePosition.Create(commitPosition), cancellationToken);
		else if (commitPosition > position.CommitPosition) // never move backwards
			position.UpdateCommitPosition(commitPosition);
		else
			return;

		await context.SaveChangesAsync(cancellationToken);
	}
}

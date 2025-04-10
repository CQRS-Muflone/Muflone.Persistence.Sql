using Muflone.Core;

namespace Muflone.Persistence.Sql.Services;

public interface IMufloneSqlPersistenceService
{
    Task<IEnumerable<ResolvedEvent>> GetAggregateStreamByIdAsync(IDomainId id, int version,
        CancellationToken cancellationToken);
}
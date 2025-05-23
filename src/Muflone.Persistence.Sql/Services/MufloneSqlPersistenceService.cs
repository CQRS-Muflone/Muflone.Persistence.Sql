﻿using Muflone.Persistence.Sql.Helpers;
using Muflone.Persistence.Sql.Persistence;

namespace Muflone.Persistence.Sql.Services;

internal sealed class MufloneSqlPersistenceService(SqlOptions sqlOptions) : IMufloneSqlPersistenceService
{
    public async Task<IEnumerable<ResolvedEvent>> GetAggregateStreamByIdAsync(
        string id,
        int version,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        await using var facade = new EventStoreFacade(sqlOptions.ConnectionString);
        var readResult = facade.GetAggregateStreamByIdAsync(id, version, cancellationToken);
        
        if (readResult.Length == 0)
            return [];
        
        IEnumerable<ResolvedEvent> resolvedEvents = new List<ResolvedEvent>();

        return readResult.Aggregate(resolvedEvents,
            (current, eventRecord) => current.Append(eventRecord.ConvertToResolvedEvent()));
    }

    public async Task<IEnumerable<ResolvedAggregate>> GetAggregatesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        await using var facade = new EventStoreFacade(sqlOptions.ConnectionString);
        var aggregates = facade.EventStore
            .Where(e => e.Version == 1)
            .Select(e => new {e.AggregateId, e.AggregateName}).Distinct();
        
        if (aggregates.ToArray().Length == 0)
            return [];

        return aggregates.Select(a => new ResolvedAggregate
        {
            AggregateId = a.AggregateId,
            AggregateName = a.AggregateName
        }).ToList();
    }
}
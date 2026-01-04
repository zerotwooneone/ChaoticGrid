using System.Collections.Concurrent;
using ChaoticGrid.Server.Domain.Aggregates.GameAggregate;

namespace ChaoticGrid.Server.Domain.Services;

public sealed class MatchManager
{
    private readonly ConcurrentDictionary<Guid, Match> _matches = new();

    public Match GetOrCreate(Guid boardId) => _matches.GetOrAdd(boardId, _ => new Match());
}

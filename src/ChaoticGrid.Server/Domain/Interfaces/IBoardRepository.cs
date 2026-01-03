using ChaoticGrid.Server.Domain.Aggregates.BoardAggregate;

namespace ChaoticGrid.Server.Domain.Interfaces;

public interface IBoardRepository
{
    Task<Board?> GetByIdAsync(BoardId id, CancellationToken cancellationToken = default);

    Task AddAsync(Board board, CancellationToken cancellationToken = default);

    Task UpdateAsync(Board board, CancellationToken cancellationToken = default);
}

using ChaoticGrid.Server.Domain.Aggregates.BoardAggregate;
using ChaoticGrid.Server.Domain.Interfaces;
using ChaoticGrid.Server.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChaoticGrid.Server.Infrastructure.Persistence.Repositories;

public sealed class SqliteBoardRepository(AppDbContext dbContext) : IBoardRepository
{
    public async Task<Board?> GetByIdAsync(BoardId id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Boards
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task AddAsync(Board board, CancellationToken cancellationToken = default)
    {
        await dbContext.Boards.AddAsync(board, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Board board, CancellationToken cancellationToken = default)
    {
        dbContext.Boards.Update(board);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

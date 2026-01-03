using ChaoticGrid.Server.Domain.Aggregates.IdentityAggregate;
using ChaoticGrid.Server.Domain.Interfaces;
using ChaoticGrid.Server.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChaoticGrid.Server.Infrastructure.Persistence.Repositories;

public sealed class SqliteUserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Set<User>()
            .Include(x => x.GlobalRoles)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        db.Set<User>().CountAsync(cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await db.Set<User>().AddAsync(user, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }
}

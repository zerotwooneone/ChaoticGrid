using ChaoticGrid.Server.Domain.Aggregates.IdentityAggregate;
using ChaoticGrid.Server.Domain.Interfaces;
using ChaoticGrid.Server.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChaoticGrid.Server.Infrastructure.Persistence.Repositories;

public sealed class SqliteRoleTemplateRepository(AppDbContext db) : IRoleTemplateRepository
{
    public async Task<IReadOnlyList<RoleTemplate>> GetByOwnerUserIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await db.Set<RoleTemplate>()
            .Where(t => t.OwnerUserId == ownerUserId)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<RoleTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return db.Set<RoleTemplate>()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task AddAsync(RoleTemplate template, CancellationToken cancellationToken = default)
    {
        await db.Set<RoleTemplate>().AddAsync(template, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(RoleTemplate template, CancellationToken cancellationToken = default)
    {
        db.Set<RoleTemplate>().Update(template);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(RoleTemplate template, CancellationToken cancellationToken = default)
    {
        db.Set<RoleTemplate>().Remove(template);
        await db.SaveChangesAsync(cancellationToken);
    }
}

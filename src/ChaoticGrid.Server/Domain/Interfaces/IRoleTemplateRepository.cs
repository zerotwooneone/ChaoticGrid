using ChaoticGrid.Server.Domain.Aggregates.IdentityAggregate;

namespace ChaoticGrid.Server.Domain.Interfaces;

public interface IRoleTemplateRepository
{
    Task<IReadOnlyList<RoleTemplate>> GetByOwnerUserIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default);

    Task<RoleTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(RoleTemplate template, CancellationToken cancellationToken = default);

    Task UpdateAsync(RoleTemplate template, CancellationToken cancellationToken = default);

    Task DeleteAsync(RoleTemplate template, CancellationToken cancellationToken = default);
}

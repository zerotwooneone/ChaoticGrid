using ChaoticGrid.Server.Domain.Enums;
using ChaoticGrid.Server.Domain.Shared;

namespace ChaoticGrid.Server.Domain.Aggregates.IdentityAggregate;

public sealed class RoleTemplate : Entity<Guid>
{
    public Guid OwnerUserId { get; private set; }

    public string Name { get; private set; }

    public BoardPermission DefaultBoardPermissions { get; private set; }

    public RoleTemplate(Guid id, Guid ownerUserId, string name, BoardPermission defaultBoardPermissions)
        : base(id)
    {
        if (ownerUserId == Guid.Empty)
        {
            throw new ArgumentException("OwnerUserId cannot be empty.", nameof(ownerUserId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Role template name cannot be empty.", nameof(name));
        }

        OwnerUserId = ownerUserId;
        Name = name.Trim();
        DefaultBoardPermissions = defaultBoardPermissions;
    }
}

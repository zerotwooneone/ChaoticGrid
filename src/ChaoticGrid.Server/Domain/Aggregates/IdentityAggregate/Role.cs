using ChaoticGrid.Server.Domain.Enums;
using ChaoticGrid.Server.Domain.Shared;

namespace ChaoticGrid.Server.Domain.Aggregates.IdentityAggregate;

public sealed class Role : Entity<Guid>
{
    public string Name { get; private set; }

    public SystemPermission Permissions { get; private set; }

    public Role(Guid id, string name, SystemPermission permissions)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Role name is required.", nameof(name));
        }

        Name = name;
        Permissions = permissions;
    }
}

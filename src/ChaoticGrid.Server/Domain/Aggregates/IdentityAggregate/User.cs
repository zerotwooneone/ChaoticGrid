using ChaoticGrid.Server.Domain.Enums;
using ChaoticGrid.Server.Domain.Shared;

namespace ChaoticGrid.Server.Domain.Aggregates.IdentityAggregate;

public sealed class User : Entity<Guid>
{
    public string Nickname { get; private set; }

    public List<Role> GlobalRoles { get; private set; } = [];

    public User(Guid id, string nickname)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(nickname))
        {
            throw new ArgumentException("Nickname is required.", nameof(nickname));
        }

        Nickname = nickname;
    }

    public GamePermission GetEffectivePermissions()
    {
        var perms = GamePermission.None;
        foreach (var role in GlobalRoles)
        {
            perms |= role.Permissions;
        }

        return perms;
    }
}

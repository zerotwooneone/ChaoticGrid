using ChaoticGrid.Server.Domain.Enums;

namespace ChaoticGrid.Server.Domain.Entities;

public sealed class BoardRole(Guid id, string name, BoardPermission defaultPermissions)
{
    public Guid Id { get; } = id;

    public string Name { get; } = string.IsNullOrWhiteSpace(name)
        ? throw new ArgumentException("Role name cannot be empty.", nameof(name))
        : name.Trim();

    public BoardPermission DefaultPermissions { get; } = defaultPermissions;

    public static BoardRole Create(string name, BoardPermission defaultPermissions)
    {
        return new BoardRole(Guid.NewGuid(), name, defaultPermissions);
    }
}

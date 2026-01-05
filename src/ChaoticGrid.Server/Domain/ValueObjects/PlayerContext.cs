using ChaoticGrid.Server.Domain.Enums;

namespace ChaoticGrid.Server.Domain.ValueObjects;

public sealed record PlayerContext(
    Guid? RoleId,
    string? RoleName,
    BoardPermission RolePermissions,
    BoardPermission AllowOverrides,
    BoardPermission DenyOverrides,
    BoardPermission EffectivePermissions);

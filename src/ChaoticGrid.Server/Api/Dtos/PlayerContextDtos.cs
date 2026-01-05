using ChaoticGrid.Server.Domain.Enums;

namespace ChaoticGrid.Server.Api.Dtos;

public sealed record PlayerContextDto(
    Guid? RoleId,
    string? RoleName,
    int RolePermissions,
    int AllowOverrides,
    int DenyOverrides,
    int EffectivePermissions);

public sealed record UpdatePermissionOverrideRequest(int AllowOverrideMask, int DenyOverrideMask);

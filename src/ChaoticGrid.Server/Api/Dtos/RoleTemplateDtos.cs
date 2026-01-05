using ChaoticGrid.Server.Domain.Enums;

namespace ChaoticGrid.Server.Api.Dtos;

public sealed record RoleTemplateDto(Guid Id, string Name, int DefaultBoardPermissions);

public sealed record CreateRoleTemplateRequest(string Name, int DefaultBoardPermissions);

public sealed record UpdateRoleTemplateRequest(string Name, int DefaultBoardPermissions);

public sealed record MySystemContextDto(string Nickname, int SystemPermissions);

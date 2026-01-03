using ChaoticGrid.Server.Domain.Enums;

namespace ChaoticGrid.Server.Domain.Services;

public interface IPermissionService
{
    bool HasPermission(Guid userId, Guid? boardId, GamePermission requiredPermission);
}

using System;

namespace ChaoticGrid.Server.Domain.Enums;

[Flags]
public enum SystemPermission
{
    None = 0,
    Login = 1 << 0,
    CreateBoard = 1 << 1,
    ManageSystemUsers = 1 << 2,
    ViewSystemLogs = 1 << 3,
    ManageSystemRoles = 1 << 4,

    All = Login
        | CreateBoard
        | ManageSystemUsers
        | ViewSystemLogs
        | ManageSystemRoles
}

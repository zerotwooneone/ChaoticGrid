using System;

namespace ChaoticGrid.Server.Domain.Enums;

[Flags]
public enum BoardPermission
{
    None = 0,
    SuggestTile = 1 << 0,
    CastVote = 1 << 1,
    ApproveTile = 1 << 2,
    ModifyBoardSettings = 1 << 3,
    ManageBoardRoles = 1 << 4,
    ForceCompleteTile = 1 << 5,
    SelfCompleteTile = 1 << 6,
    ModifyOwnPermissions = 1 << 7
}

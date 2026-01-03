using System;

namespace ChaoticGrid.Server.Domain.Enums;

[Flags]
public enum GamePermission
{
    None = 0,
    CreateBoard = 1 << 0,
    ManageSystem = 1 << 1,
    SuggestTile = 1 << 2,
    ApproveTile = 1 << 3,
    CastVote = 1 << 4,
    ForceConfirm = 1 << 5,
    SelfConfirm = 1 << 6,
    ModifyBoard = 1 << 7,
    ManageBoardRoles = 1 << 8,

    All = CreateBoard
        | ManageSystem
        | SuggestTile
        | ApproveTile
        | CastVote
        | ForceConfirm
        | SelfConfirm
        | ModifyBoard
        | ManageBoardRoles
}

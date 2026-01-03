namespace ChaoticGrid.Server.Domain.ValueObjects;

public readonly record struct PermissionSet(
    bool CanView,
    bool CanEditBoard,
    bool CanStart,
    bool CanFinish,
    bool CanSuggestTiles,
    bool CanApproveTiles)
{
    public static PermissionSet None => new(
        CanView: false,
        CanEditBoard: false,
        CanStart: false,
        CanFinish: false,
        CanSuggestTiles: false,
        CanApproveTiles: false);
}

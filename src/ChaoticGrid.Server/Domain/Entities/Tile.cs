using ChaoticGrid.Server.Domain.Aggregates.BoardAggregate;
using ChaoticGrid.Server.Domain.Enums;

namespace ChaoticGrid.Server.Domain.Entities;

public sealed record Tile(Guid Id, BoardId BoardId, string Text, TileStatus Status, PlayerId CreatedByPlayerId, bool IsConfirmed)
{
    public bool IsApproved => Status == TileStatus.Approved;

    public static Tile CreateSuggestion(BoardId boardId, string text, PlayerId createdByPlayerId)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Tile text cannot be empty.", nameof(text));
        }

        if (createdByPlayerId.Value == Guid.Empty)
        {
            throw new ArgumentException("CreatedByPlayerId is required.", nameof(createdByPlayerId));
        }

        var normalized = text.Trim();
        return new Tile(Guid.NewGuid(), boardId, normalized, TileStatus.Pending, createdByPlayerId, IsConfirmed: false);
    }

    public Tile Approve() => this with { Status = TileStatus.Approved };

    public Tile Reject() => this with { Status = TileStatus.Rejected };

    public Tile Confirm() => this with { IsConfirmed = true };
}

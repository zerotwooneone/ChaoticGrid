using ChaoticGrid.Server.Domain.Aggregates.BoardAggregate;

namespace ChaoticGrid.Server.Domain.ValueObjects;

public sealed record Vote(PlayerId PlayerId, Guid TileId, DateTimeOffset CastAt)
{
    public static Vote Create(PlayerId playerId, Guid tileId, DateTimeOffset? castAt = null)
    {
        if (playerId.Value == Guid.Empty)
        {
            throw new ArgumentException("PlayerId cannot be empty.", nameof(playerId));
        }

        if (tileId == Guid.Empty)
        {
            throw new ArgumentException("TileId cannot be empty.", nameof(tileId));
        }

        return new Vote(playerId, tileId, castAt ?? DateTimeOffset.UtcNow);
    }
}

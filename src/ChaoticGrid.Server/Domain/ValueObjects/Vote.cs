namespace ChaoticGrid.Server.Domain.ValueObjects;

public sealed record Vote(Guid PlayerId, Guid TileId, DateTimeOffset CastAt)
{
    public static Vote Create(Guid playerId, Guid tileId, DateTimeOffset? castAt = null)
    {
        if (playerId == Guid.Empty)
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

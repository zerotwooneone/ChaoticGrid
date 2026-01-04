using ChaoticGrid.Server.Domain.Aggregates.BoardAggregate;
using ChaoticGrid.Server.Domain.Aggregates.IdentityAggregate;

namespace ChaoticGrid.Server.Domain.Entities;

public sealed class Player(PlayerId id, UserId ownerUserId, string displayName)
{
    public List<Guid> GridTileIds { get; private set; } = [];

    public List<string> Roles { get; private set; } = [];

    public DateTime? SilencedUntilUtc { get; private set; }

    public PlayerId Id { get; } = id;

    public UserId OwnerUserId { get; } = ownerUserId;

    public string DisplayName { get; } = string.IsNullOrWhiteSpace(displayName)
        ? throw new ArgumentException("Display name cannot be empty.", nameof(displayName))
        : displayName.Trim();

    public bool IsHost => Roles.Contains("Host");

    public bool IsSilenced(DateTime utcNow) => SilencedUntilUtc is not null && SilencedUntilUtc.Value > utcNow;

    public static Player Create(PlayerId id, UserId ownerUserId, string displayName, bool isHost = false)
    {
        var player = new Player(id, ownerUserId, displayName);
        if (isHost)
        {
            player.Roles.Add("Host");
        }

        return player;
    }

    public void SilenceUntil(DateTime utcUntil)
    {
        SilencedUntilUtc = utcUntil;
    }

    public void AssignRandomizedGrid(IReadOnlyCollection<Tile> approvedTiles, int? seed = null)
    {
        if (approvedTiles is null)
        {
            throw new ArgumentNullException(nameof(approvedTiles));
        }

        var tiles = approvedTiles.Where(t => t.IsApproved).ToArray();

        if (tiles.Length < 24)
        {
            throw new InvalidOperationException("At least 24 approved tiles are required to generate a 5x5 grid.");
        }

        var rng = seed is null ? Random.Shared : new Random(seed.Value);

        // Fisher-Yates shuffle over a copy.
        var shuffled = tiles.ToArray();
        for (var i = shuffled.Length - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        GridTileIds.Clear();
        for (var i = 0; i < 24; i++)
        {
            GridTileIds.Add(shuffled[i].Id);
        }

        GridTileIds.Insert(12, Guid.Empty);
    }
}

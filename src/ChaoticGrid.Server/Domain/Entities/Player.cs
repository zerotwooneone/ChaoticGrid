using ChaoticGrid.Server.Domain.Entities;

namespace ChaoticGrid.Server.Domain.Entities;

public sealed class Player(Guid id, string displayName)
{
    private readonly Tile?[] grid = new Tile?[25];

    public Guid Id { get; } = id;

    public string DisplayName { get; } = string.IsNullOrWhiteSpace(displayName)
        ? throw new ArgumentException("Display name cannot be empty.", nameof(displayName))
        : displayName.Trim();

    public IReadOnlyList<Tile?> Grid => this.grid;

    public bool IsHost { get; private set; }

    public static Player Create(Guid id, string displayName, bool isHost = false)
    {
        var player = new Player(id, displayName);
        player.IsHost = isHost;
        return player;
    }

    public void AssignRandomizedGrid(IReadOnlyCollection<Tile> approvedTiles, int? seed = null)
    {
        if (approvedTiles is null)
        {
            throw new ArgumentNullException(nameof(approvedTiles));
        }

        var tiles = approvedTiles.Where(t => t.IsApproved).ToArray();

        if (tiles.Length < 25)
        {
            throw new InvalidOperationException("At least 25 approved tiles are required to generate a 5x5 grid.");
        }

        var rng = seed is null ? Random.Shared : new Random(seed.Value);

        // Fisher-Yates shuffle over a copy.
        var shuffled = tiles.ToArray();
        for (var i = shuffled.Length - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        Array.Clear(this.grid);
        for (var i = 0; i < 25; i++)
        {
            this.grid[i] = shuffled[i];
        }
    }
}

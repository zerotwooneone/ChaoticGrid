using ChaoticGrid.Server.Domain.Entities;
using ChaoticGrid.Server.Domain.Shared;
using ChaoticGrid.Server.Domain.ValueObjects;

namespace ChaoticGrid.Server.Domain.Aggregates.BoardAggregate;

public sealed class Board(BoardId id, string name) : Entity<BoardId>(id)
{
    public string Name { get; private set; } = string.IsNullOrWhiteSpace(name)
        ? throw new ArgumentException("Board name cannot be empty.", nameof(name))
        : name.Trim();

    public BoardStatus Status { get; private set; } = BoardStatus.Draft;

    public List<Tile> Tiles { get; private set; } = [];

    public List<Player> Players { get; private set; } = [];

    public int MinimumApprovedTilesToStart { get; private set; } = 25;

    public static Board Create(string name, int minimumApprovedTilesToStart = 25)
    {
        var board = new Board(BoardId.New(), name);
        board.SetMinimumApprovedTilesToStart(minimumApprovedTilesToStart);
        return board;
    }

    public void Rename(string name)
    {
        EnsureDraft();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Board name cannot be empty.", nameof(name));
        }

        Name = name.Trim();
    }

    public void SetMinimumApprovedTilesToStart(int minimumApprovedTilesToStart)
    {
        EnsureDraft();

        if (minimumApprovedTilesToStart <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumApprovedTilesToStart));
        }

        MinimumApprovedTilesToStart = minimumApprovedTilesToStart;
    }

    public Tile AddTileSuggestion(string text)
    {
        EnsureNotFinished();

        var tile = Tile.Create(text);
        Tiles.Add(tile);
        return tile;
    }

    public void ApproveTile(Guid tileId)
    {
        EnsureNotFinished();

        var index = Tiles.FindIndex(t => t.Id == tileId);
        if (index < 0)
        {
            throw new InvalidOperationException("Tile does not exist.");
        }

        Tiles[index] = Tiles[index].Approve();
    }

    public void RemoveTile(Guid tileId)
    {
        EnsureDraft();

        var removed = Tiles.RemoveAll(t => t.Id == tileId);
        if (removed == 0)
        {
            throw new InvalidOperationException("Tile does not exist.");
        }
    }

    public Player AddPlayer(Guid playerId, string displayName, bool isHost = false, int? seed = null)
    {
        EnsureNotFinished();

        if (Players.Any(p => p.Id == playerId))
        {
            throw new InvalidOperationException("Player already exists.");
        }

        var player = Player.Create(playerId, displayName, isHost);
        Players.Add(player);

        if (Status == BoardStatus.Active)
        {
            player.AssignRandomizedGrid(GetApprovedTiles(), seed);
        }

        return player;
    }

    public void Start(int? seed = null)
    {
        EnsureDraft();

        var approved = GetApprovedTiles();
        if (approved.Count < MinimumApprovedTilesToStart)
        {
            throw new InvalidOperationException($"Cannot start: requires at least {MinimumApprovedTilesToStart} approved tiles.");
        }

        foreach (var player in Players)
        {
            player.AssignRandomizedGrid(approved, seed is null ? null : seed + player.Id.GetHashCode());
        }

        Status = BoardStatus.Active;
    }

    public void Finish()
    {
        if (Status != BoardStatus.Active)
        {
            throw new InvalidOperationException("Board can only be finished from Active state.");
        }

        Status = BoardStatus.Finished;
    }

    public PermissionSet GetPermissions(Guid? userId)
    {
        if (userId is null)
        {
            return new PermissionSet(
                CanView: true,
                CanEditBoard: false,
                CanStart: false,
                CanFinish: false,
                CanSuggestTiles: false,
                CanApproveTiles: false);
        }

        var player = Players.FirstOrDefault(p => p.Id == userId.Value);
        if (player is null)
        {
            return new PermissionSet(
                CanView: true,
                CanEditBoard: false,
                CanStart: false,
                CanFinish: false,
                CanSuggestTiles: true,
                CanApproveTiles: false);
        }

        var isHost = player.IsHost;

        return new PermissionSet(
            CanView: true,
            CanEditBoard: isHost && Status == BoardStatus.Draft,
            CanStart: isHost && Status == BoardStatus.Draft,
            CanFinish: isHost && Status == BoardStatus.Active,
            CanSuggestTiles: Status != BoardStatus.Finished,
            CanApproveTiles: isHost && Status != BoardStatus.Finished);
    }

    private IReadOnlyCollection<Tile> GetApprovedTiles() => Tiles.Where(t => t.IsApproved).ToArray();

    private void EnsureDraft()
    {
        if (Status != BoardStatus.Draft)
        {
            throw new InvalidOperationException("Operation only allowed while board is Draft.");
        }
    }

    private void EnsureNotFinished()
    {
        if (Status == BoardStatus.Finished)
        {
            throw new InvalidOperationException("Operation not allowed while board is Finished.");
        }
    }
}

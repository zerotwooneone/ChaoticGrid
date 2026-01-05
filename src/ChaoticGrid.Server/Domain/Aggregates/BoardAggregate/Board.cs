using ChaoticGrid.Server.Domain.Entities;
using ChaoticGrid.Server.Domain.Aggregates.IdentityAggregate;
using ChaoticGrid.Server.Domain.Enums;
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

    public List<BoardRole> BoardRoles { get; private set; } = [];

    public int MinimumApprovedTilesToStart { get; private set; } = 24;

    public int VotesRequiredToConfirm { get; private set; } = 2;

    public List<Vote> Votes { get; private set; } = [];

    public static Board Create(string name, int minimumApprovedTilesToStart = 24)
    {
        var board = new Board(BoardId.New(), name);
        board.SetMinimumApprovedTilesToStart(minimumApprovedTilesToStart);
        return board;
    }

    public BoardRole CreateRole(string name, BoardPermission defaultPermissions)
    {
        EnsureNotFinished();

        var role = BoardRole.Create(name, defaultPermissions);
        BoardRoles.Add(role);
        return role;
    }

    public void AssignRole(UserId userId, Guid roleId)
    {
        EnsureNotFinished();

        var player = Players.FirstOrDefault(p => p.OwnerUserId == userId)
            ?? throw new InvalidOperationException("Player does not exist.");

        if (!BoardRoles.Any(r => r.Id == roleId))
        {
            throw new InvalidOperationException("Role does not exist.");
        }

        player.AssignRole(roleId);
    }

    public PlayerContext GetMyContext(UserId userId)
    {
        var player = Players.FirstOrDefault(p => p.OwnerUserId == userId)
            ?? throw new InvalidOperationException("Player does not exist.");

        BoardRole? role = null;
        if (player.AssignedRoleId is not null)
        {
            role = BoardRoles.FirstOrDefault(r => r.Id == player.AssignedRoleId.Value);
        }

        var rolePermissions = role?.DefaultPermissions ?? BoardPermission.None;
        var effective = player.GetEffectivePermissions(rolePermissions);

        return new PlayerContext(
            RoleId: role?.Id,
            RoleName: role?.Name,
            RolePermissions: rolePermissions,
            AllowOverrides: player.AllowPermissionOverrides,
            DenyOverrides: player.DenyPermissionOverrides,
            EffectivePermissions: effective);
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

    public Tile AddTileSuggestion(UserId userId, string text)
    {
        EnsureNotFinished();

        var player = Players.FirstOrDefault(p => p.OwnerUserId == userId)
            ?? throw new InvalidOperationException("Player does not exist.");

        if (player.IsSilenced(DateTime.UtcNow))
        {
            throw new InvalidOperationException("Player is silenced.");
        }

        var tile = Tile.CreateSuggestion(Id, text, player.Id);
        Tiles.Add(tile);
        return tile;
    }

    public void RejectTile(Guid tileId, DateTime utcNow, TimeSpan silenceDuration)
    {
        EnsureNotFinished();

        var index = Tiles.FindIndex(t => t.Id == tileId);
        if (index < 0)
        {
            throw new InvalidOperationException("Tile does not exist.");
        }

        var tile = Tiles[index];
        Tiles[index] = tile.Reject();

        var proposer = Players.FirstOrDefault(p => p.Id == tile.CreatedByPlayerId);
        if (proposer is not null)
        {
            proposer.SilenceUntil(utcNow.Add(silenceDuration));
        }
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

    public void CastVote(PlayerId playerId, Guid tileId, DateTime utcNow)
    {
        EnsureNotFinished();

        var player = Players.FirstOrDefault(p => p.Id == playerId) ?? throw new InvalidOperationException("Player does not exist.");

        if (player.IsSilenced(utcNow))
        {
            throw new InvalidOperationException("Player is silenced.");
        }

        var index = Tiles.FindIndex(t => t.Id == tileId);
        if (index < 0)
        {
            throw new InvalidOperationException("Tile does not exist.");
        }

        var tile = Tiles[index];
        if (tile.IsConfirmed)
        {
            return;
        }

        if (Votes.Any(v => v.PlayerId == playerId && v.TileId == tileId))
        {
            throw new InvalidOperationException("Player cannot vote twice on the same tile.");
        }

        Votes.Add(Vote.Create(playerId, tileId, new DateTimeOffset(utcNow, TimeSpan.Zero)));

        var voteCount = Votes.Count(v => v.TileId == tileId);
        if (voteCount >= VotesRequiredToConfirm)
        {
            Tiles[index] = tile.Confirm();
        }
    }

    public void ConfirmTile(Guid tileId)
    {
        EnsureNotFinished();

        var index = Tiles.FindIndex(t => t.Id == tileId);
        if (index < 0)
        {
            throw new InvalidOperationException("Tile does not exist.");
        }

        var tile = Tiles[index];
        if (tile.IsConfirmed)
        {
            return;
        }

        Tiles[index] = tile.Confirm();
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

    public Player Join(UserId userId, string displayName, bool isHost = false, int? seed = null)
    {
        EnsureNotFinished();

        var existing = Players.FirstOrDefault(p => p.OwnerUserId == userId);
        if (existing is not null)
        {
            return existing;
        }

        var player = Player.Create(PlayerId.New(), userId, displayName, isHost);
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
            player.AssignRandomizedGrid(approved, seed is null ? null : seed + player.Id.Value.GetHashCode());
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

    public PermissionSet GetPermissions(UserId? userId)
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

        var player = Players.FirstOrDefault(p => p.OwnerUserId == userId.Value);
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

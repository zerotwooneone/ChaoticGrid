using ChaoticGrid.Server.Api.Dtos;
using ChaoticGrid.Server.Domain.Aggregates.BoardAggregate;
using ChaoticGrid.Server.Domain.Aggregates.IdentityAggregate;
using ChaoticGrid.Server.Domain.Enums;
using ChaoticGrid.Server.Domain.Interfaces;
using ChaoticGrid.Server.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ChaoticGrid.Server.Infrastructure.Hubs;

[Authorize]
public sealed class GameHub(IBoardRepository repo, MatchManager matches) : Hub<IGameClient>
{
    public async Task<Guid> JoinBoard(Guid boardId, string displayName, bool isHost = false, int? seed = null)
    {
        if (!TryGetUserId(Context.User, out var userId))
        {
            throw new HubException("Unauthorized.");
        }

        var board = await repo.GetByIdAsync(new BoardId(boardId), CancellationToken.None);
        if (board is null)
        {
            throw new HubException("Board not found.");
        }

        if (!board.GetMyContext(userId).EffectivePermissions.HasFlag(BoardPermission.SuggestTile))
        {
            throw new HubException("Forbidden.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(boardId));

        var player = board.Join(userId, displayName, isHost, seed);
        await repo.UpdateAsync(board, CancellationToken.None);

        await Clients.Caller.BoardStateUpdated(ToDto(board));
        await Clients.Group(GetGroupName(boardId)).BoardStateUpdated(ToDto(board));

        return player.Id.Value;
    }

    public async Task ProposeCompletion(Guid boardId, Guid tileId)
    {
        if (!TryGetUserId(Context.User, out var userId))
        {
            throw new HubException("Unauthorized.");
        }

        var board = await repo.GetByIdAsync(new BoardId(boardId), CancellationToken.None);
        if (board is null)
        {
            throw new HubException("Board not found.");
        }

        var utcNow = DateTime.UtcNow;
        var proposer = board.Players.FirstOrDefault(p => p.OwnerUserId == userId) ?? throw new HubException("Player not found.");
        var playerId = proposer.Id.Value;

        if (proposer.IsSilenced(utcNow))
        {
            throw new HubException("Player is silenced.");
        }

        var match = matches.GetOrCreate(boardId);

        var perms = board.GetMyContext(userId).EffectivePermissions;
        if (perms.HasFlag(BoardPermission.ForceCompleteTile))
        {
            var resolution = match.ForceConfirm(playerId, tileId);
            board.ConfirmTile(resolution.TileId);
            await repo.UpdateAsync(board, CancellationToken.None);

            await Clients.Group(GetGroupName(boardId)).TileConfirmed(new TileConfirmedDto(resolution.TileId));
            await Clients.Group(GetGroupName(boardId)).BoardStateUpdated(ToDto(board));
            return;
        }

        match.StartVote(playerId, tileId, board.Players.Count, utcNow, TimeSpan.FromMinutes(2));

        await Clients.Group(GetGroupName(boardId)).VoteRequested(new CompletionVoteStartedDto(tileId, playerId));
    }

    public async Task CastCompletionVote(Guid boardId, CompletionVoteRequest vote)
    {
        if (!TryGetUserId(Context.User, out var userId))
        {
            throw new HubException("Unauthorized.");
        }

        var board = await repo.GetByIdAsync(new BoardId(boardId), CancellationToken.None);
        if (board is null)
        {
            throw new HubException("Board not found.");
        }

        var utcNow = DateTime.UtcNow;
        var voter = board.Players.FirstOrDefault(p => p.OwnerUserId == userId) ?? throw new HubException("Player not found.");
        var playerId = voter.Id.Value;
        if (voter.IsSilenced(utcNow))
        {
            throw new HubException("Player is silenced.");
        }

        var match = matches.GetOrCreate(boardId);
        var resolution = match.AddVote(playerId, vote.IsYes, utcNow);

        if (resolution.Type == Domain.Aggregates.GameAggregate.Match.VoteResolutionType.Confirmed)
        {
            board.ConfirmTile(resolution.TileId);
            await repo.UpdateAsync(board, CancellationToken.None);

            await Clients.Group(GetGroupName(boardId)).TileConfirmed(new TileConfirmedDto(resolution.TileId));
            await Clients.Group(GetGroupName(boardId)).BoardStateUpdated(ToDto(board));
        }

        if (resolution.Type == Domain.Aggregates.GameAggregate.Match.VoteResolutionType.Rejected && resolution.ProposerId is not null && resolution.SilencedUntilUtc is not null)
        {
            var proposer = board.Players.FirstOrDefault(p => p.Id.Value == resolution.ProposerId.Value);
            if (proposer is not null)
            {
                proposer.SilenceUntil(resolution.SilencedUntilUtc.Value);
                await repo.UpdateAsync(board, CancellationToken.None);
                await Clients.Group(GetGroupName(boardId)).BoardStateUpdated(ToDto(board));
            }
        }
    }

    public async Task ProposeTile(Guid boardId, string text)
    {
        if (!TryGetUserId(Context.User, out var userId))
        {
            throw new HubException("Unauthorized.");
        }

        var board = await repo.GetByIdAsync(new BoardId(boardId), CancellationToken.None);
        if (board is null)
        {
            throw new HubException("Board not found.");
        }

        var tile = board.AddTileSuggestion(userId, text);
        await repo.UpdateAsync(board, CancellationToken.None);

        await Clients.Group(GetGroupName(boardId)).TileSuggested(new TileDto(tile.Id, tile.Text, tile.IsApproved, tile.IsConfirmed, tile.Status, tile.CreatedByPlayerId.Value));
        await Clients.Group(GetGroupName(boardId)).BoardStateUpdated(ToDto(board));
    }

    public async Task CastVote(Guid boardId, Guid tileId)
    {
        if (!TryGetUserId(Context.User, out var userId))
        {
            throw new HubException("Unauthorized.");
        }

        var board = await repo.GetByIdAsync(new BoardId(boardId), CancellationToken.None);
        if (board is null)
        {
            throw new HubException("Board not found.");
        }

        var voter = board.Players.FirstOrDefault(p => p.OwnerUserId == userId) ?? throw new HubException("Player not found.");
        var playerId = voter.Id;

        board.CastVote(playerId, tileId, DateTime.UtcNow);
        await repo.UpdateAsync(board, CancellationToken.None);

        await Clients.Group(GetGroupName(boardId)).VoteCast(new VoteRequest(playerId.Value, tileId));
        await Clients.Group(GetGroupName(boardId)).BoardStateUpdated(ToDto(board));
    }

    private static string GetGroupName(Guid boardId) => $"board:{boardId:D}";

    private static BoardStateDto ToDto(Board board)
    {
        return new BoardStateDto(
            BoardId: board.Id.Value,
            Name: board.Name,
            Status: board.Status,
            MinimumApprovedTilesToStart: board.MinimumApprovedTilesToStart,
            Tiles: board.Tiles.Select(t => new TileDto(t.Id, t.Text, t.IsApproved, t.IsConfirmed, t.Status, t.CreatedByPlayerId.Value)).ToArray(),
            Players: board.Players.Select(p => new PlayerDto(p.Id.Value, p.DisplayName, p.GridTileIds.ToArray(), p.Roles.ToArray(), p.SilencedUntilUtc)).ToArray());
    }

    private static bool TryGetUserId(ClaimsPrincipal? user, out UserId userId)
    {
        userId = default;

        var raw = user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? user?.FindFirstValue("sub");
        return Guid.TryParse(raw, out var parsed) && parsed != Guid.Empty && (userId = new UserId(parsed)) != default;
    }

}

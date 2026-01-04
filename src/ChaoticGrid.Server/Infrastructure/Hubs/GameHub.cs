using ChaoticGrid.Server.Api.Dtos;
using ChaoticGrid.Server.Domain.Aggregates.BoardAggregate;
using ChaoticGrid.Server.Domain.Enums;
using ChaoticGrid.Server.Domain.Interfaces;
using ChaoticGrid.Server.Domain.Services;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ChaoticGrid.Server.Infrastructure.Hubs;

public sealed class GameHub(IBoardRepository repo, MatchManager matches) : Hub<IGameClient>
{
    public async Task JoinBoard(Guid boardId, Guid playerId, string displayName, bool isHost = false, int? seed = null)
    {
        var board = await repo.GetByIdAsync(new BoardId(boardId), CancellationToken.None);
        if (board is null)
        {
            throw new HubException("Board not found.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(boardId));

        if (!board.Players.Any(p => p.Id == playerId))
        {
            board.AddPlayer(playerId, displayName, isHost, seed);
            await repo.UpdateAsync(board, CancellationToken.None);
        }

        await Clients.Caller.BoardStateUpdated(ToDto(board));
        await Clients.Group(GetGroupName(boardId)).BoardStateUpdated(ToDto(board));
    }

    public async Task ProposeCompletion(Guid boardId, Guid playerId, Guid tileId)
    {
        var board = await repo.GetByIdAsync(new BoardId(boardId), CancellationToken.None);
        if (board is null)
        {
            throw new HubException("Board not found.");
        }

        var utcNow = DateTime.UtcNow;
        var proposer = board.Players.FirstOrDefault(p => p.Id == playerId) ?? throw new HubException("Player not found.");

        if (proposer.IsSilenced(utcNow))
        {
            throw new HubException("Player is silenced.");
        }

        var match = matches.GetOrCreate(boardId);

        if (HasPermission(Context.User, GamePermission.ForceConfirm))
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

    public async Task CastCompletionVote(Guid boardId, Guid playerId, CompletionVoteRequest vote)
    {
        var board = await repo.GetByIdAsync(new BoardId(boardId), CancellationToken.None);
        if (board is null)
        {
            throw new HubException("Board not found.");
        }

        var utcNow = DateTime.UtcNow;
        var voter = board.Players.FirstOrDefault(p => p.Id == playerId) ?? throw new HubException("Player not found.");
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
            var proposer = board.Players.FirstOrDefault(p => p.Id == resolution.ProposerId.Value);
            if (proposer is not null)
            {
                proposer.SilenceUntil(resolution.SilencedUntilUtc.Value);
                await repo.UpdateAsync(board, CancellationToken.None);
                await Clients.Group(GetGroupName(boardId)).BoardStateUpdated(ToDto(board));
            }
        }
    }

    public async Task ProposeTile(Guid boardId, Guid playerId, string text)
    {
        var board = await repo.GetByIdAsync(new BoardId(boardId), CancellationToken.None);
        if (board is null)
        {
            throw new HubException("Board not found.");
        }

        var tile = board.AddTileSuggestion(playerId, text);
        await repo.UpdateAsync(board, CancellationToken.None);

        await Clients.Group(GetGroupName(boardId)).TileSuggested(new TileDto(tile.Id, tile.Text, tile.IsApproved, tile.IsConfirmed, tile.Status, tile.CreatedByUserId));
        await Clients.Group(GetGroupName(boardId)).BoardStateUpdated(ToDto(board));
    }

    public async Task CastVote(Guid boardId, VoteRequest vote)
    {
        var board = await repo.GetByIdAsync(new BoardId(boardId), CancellationToken.None);
        if (board is null)
        {
            throw new HubException("Board not found.");
        }

        board.CastVote(vote.PlayerId, vote.TileId, DateTime.UtcNow);
        await repo.UpdateAsync(board, CancellationToken.None);

        await Clients.Group(GetGroupName(boardId)).VoteCast(vote);
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
            Tiles: board.Tiles.Select(t => new TileDto(t.Id, t.Text, t.IsApproved, t.IsConfirmed, t.Status, t.CreatedByUserId)).ToArray(),
            Players: board.Players.Select(p => new PlayerDto(p.Id, p.DisplayName, p.GridTileIds.ToArray(), p.Roles.ToArray(), p.SilencedUntilUtc)).ToArray());
    }

    private static bool HasPermission(ClaimsPrincipal? user, GamePermission required)
    {
        if (user is null)
        {
            return false;
        }

        var raw = user.FindFirstValue("x-permissions");
        if (!long.TryParse(raw, out var perms))
        {
            return false;
        }

        return (((GamePermission)perms) & required) == required;
    }
}

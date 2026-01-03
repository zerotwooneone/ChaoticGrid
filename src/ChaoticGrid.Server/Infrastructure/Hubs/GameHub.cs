using ChaoticGrid.Server.Api.Dtos;
using ChaoticGrid.Server.Domain.Aggregates.BoardAggregate;
using ChaoticGrid.Server.Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace ChaoticGrid.Server.Infrastructure.Hubs;

public sealed class GameHub(IBoardRepository repo) : Hub<IGameClient>
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

    public async Task ProposeTile(Guid boardId, string text)
    {
        var board = await repo.GetByIdAsync(new BoardId(boardId), CancellationToken.None);
        if (board is null)
        {
            throw new HubException("Board not found.");
        }

        board.AddTileSuggestion(text);
        await repo.UpdateAsync(board, CancellationToken.None);

        await Clients.Group(GetGroupName(boardId)).BoardStateUpdated(ToDto(board));
    }

    public async Task CastVote(Guid boardId, VoteRequest vote)
    {
        var board = await repo.GetByIdAsync(new BoardId(boardId), CancellationToken.None);
        if (board is null)
        {
            throw new HubException("Board not found.");
        }

        // Voting rules live in Match/Game aggregate (future chunk). For now broadcast the vote event.
        await Clients.Group(GetGroupName(boardId)).VoteCast(vote);
    }

    private static string GetGroupName(Guid boardId) => $"board:{boardId:D}";

    private static BoardStateDto ToDto(Board board)
    {
        return new BoardStateDto(
            BoardId: board.Id.Value,
            Name: board.Name,
            Status: board.Status,
            MinimumApprovedTilesToStart: board.MinimumApprovedTilesToStart,
            Tiles: board.Tiles.Select(t => new TileDto(t.Id, t.Text, t.IsApproved)).ToArray(),
            Players: board.Players.Select(p => new PlayerDto(p.Id, p.DisplayName, p.GridTileIds.ToArray(), p.Roles.ToArray())).ToArray());
    }
}

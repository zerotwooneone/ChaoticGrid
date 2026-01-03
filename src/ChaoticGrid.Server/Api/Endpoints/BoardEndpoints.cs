using ChaoticGrid.Server.Api.Dtos;
using ChaoticGrid.Server.Domain.Aggregates.BoardAggregate;
using ChaoticGrid.Server.Domain.Interfaces;

namespace ChaoticGrid.Server.Api.Endpoints;

public static class BoardEndpoints
{
    public static IEndpointRouteBuilder MapBoardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/boards");

        group.MapPost("/", CreateBoard);
        group.MapPost("/{boardId:guid}/join", JoinBoard);
        group.MapGet("/{boardId:guid}", GetBoardState);

        return endpoints;
    }

    private static async Task<IResult> CreateBoard(CreateBoardRequest request, IBoardRepository repo, CancellationToken ct)
    {
        var board = Board.Create(request.Name);
        await repo.AddAsync(board, ct);
        return Results.Ok(ToDto(board));
    }

    private static async Task<IResult> JoinBoard(Guid boardId, JoinBoardRequest request, IBoardRepository repo, CancellationToken ct)
    {
        var board = await repo.GetByIdAsync(new BoardId(boardId), ct);
        if (board is null)
        {
            return Results.NotFound();
        }

        board.AddPlayer(request.PlayerId, request.DisplayName, isHost: request.IsHost, seed: request.Seed);
        await repo.UpdateAsync(board, ct);

        return Results.Ok(ToDto(board));
    }

    private static async Task<IResult> GetBoardState(Guid boardId, IBoardRepository repo, CancellationToken ct)
    {
        var board = await repo.GetByIdAsync(new BoardId(boardId), ct);
        return board is null ? Results.NotFound() : Results.Ok(ToDto(board));
    }

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

    public sealed record JoinBoardRequest(Guid PlayerId, string DisplayName, bool IsHost, int? Seed);
}

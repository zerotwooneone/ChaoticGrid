using ChaoticGrid.Server.Api.Dtos;
using ChaoticGrid.Server.Domain.Aggregates.BoardAggregate;
using ChaoticGrid.Server.Domain.Enums;
using ChaoticGrid.Server.Domain.Interfaces;
using ChaoticGrid.Server.Infrastructure.Hubs;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using ChaoticGrid.Server.Domain.Entities;

namespace ChaoticGrid.Server.Api.Endpoints;

public static class TileEndpoints
{
    public static IEndpointRouteBuilder MapTileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/tiles").RequireAuthorization();

        group.MapPost("/", SuggestTile);
        group.MapPut("/{tileId:guid}", ModerateTile);
        group.MapGet("/", GetTiles);

        return endpoints;
    }

    private static async Task<Results<Ok<TileDto>, NotFound, BadRequest, ForbidHttpResult>> SuggestTile(
        SuggestTileRequest request,
        ClaimsPrincipal user,
        IBoardRepository boards,
        IHubContext<GameHub, IGameClient> hub,
        CancellationToken ct)
    {
        if (!HasPermission(user, GamePermission.SuggestTile))
        {
            return TypedResults.Forbid();
        }

        if (!TryGetUserId(user, out var userId))
        {
            return TypedResults.Forbid();
        }

        var board = await boards.GetByIdAsync(new BoardId(request.BoardId), ct);
        if (board is null)
        {
            return TypedResults.NotFound();
        }

        Tile tile;
        try
        {
            tile = board.AddTileSuggestion(userId, request.Text);
        }
        catch
        {
            return TypedResults.BadRequest();
        }

        await boards.UpdateAsync(board, ct);

        var dto = new TileDto(tile.Id, tile.Text, tile.IsApproved, tile.IsConfirmed, tile.Status, tile.CreatedByUserId);

        // Broadcast: tile suggested (intended for approvers/admins). We'll broadcast to the board group for now.
        await hub.Clients.Group(GetGroupName(request.BoardId)).TileSuggested(dto);
        await hub.Clients.Group(GetGroupName(request.BoardId)).BoardStateUpdated(ToBoardDto(board));

        return TypedResults.Ok(dto);
    }

    private static async Task<Results<Ok<TileDto>, NotFound, BadRequest, ForbidHttpResult>> ModerateTile(
        Guid tileId,
        ModerateTileRequest request,
        ClaimsPrincipal user,
        IBoardRepository boards,
        IHubContext<GameHub, IGameClient> hub,
        CancellationToken ct)
    {
        if (!HasPermission(user, GamePermission.ApproveTile))
        {
            return TypedResults.Forbid();
        }

        var board = await boards.GetByIdAsync(new BoardId(request.BoardId), ct);
        if (board is null)
        {
            return TypedResults.NotFound();
        }

        try
        {
            if (request.Status == TileStatus.Approved)
            {
                board.ApproveTile(tileId);
            }
            else if (request.Status == TileStatus.Rejected)
            {
                board.RejectTile(tileId, DateTime.UtcNow, TimeSpan.FromMinutes(2));
            }
            else
            {
                return TypedResults.BadRequest();
            }
        }
        catch
        {
            return TypedResults.BadRequest();
        }

        await boards.UpdateAsync(board, ct);

        var tile = board.Tiles.First(t => t.Id == tileId);
        var dto = new TileDto(tile.Id, tile.Text, tile.IsApproved, tile.IsConfirmed, tile.Status, tile.CreatedByUserId);

        await hub.Clients.Group(GetGroupName(request.BoardId)).TileModerated(dto);
        await hub.Clients.Group(GetGroupName(request.BoardId)).BoardStateUpdated(ToBoardDto(board));

        return TypedResults.Ok(dto);
    }

    private static async Task<Results<Ok<TileDto[]>, NotFound, ForbidHttpResult>> GetTiles(
        Guid boardId,
        ClaimsPrincipal user,
        IBoardRepository boards,
        CancellationToken ct)
    {
        if (!TryGetUserId(user, out _))
        {
            return TypedResults.Forbid();
        }

        var board = await boards.GetByIdAsync(new BoardId(boardId), ct);
        if (board is null)
        {
            return TypedResults.NotFound();
        }

        var canApprove = HasPermission(user, GamePermission.ApproveTile);
        var tiles = canApprove
            ? board.Tiles
            : board.Tiles.Where(t => t.Status != TileStatus.Pending).ToList();

        var result = tiles
            .Select(t => new TileDto(t.Id, t.Text, t.IsApproved, t.IsConfirmed, t.Status, t.CreatedByUserId))
            .ToArray();

        return TypedResults.Ok(result);
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
    {
        userId = Guid.Empty;
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(raw, out userId);
    }

    private static bool HasPermission(ClaimsPrincipal user, GamePermission required)
    {
        var raw = user.FindFirstValue("x-permissions");
        if (!long.TryParse(raw, out var perms))
        {
            return false;
        }

        return (((GamePermission)perms) & required) == required;
    }

    private static string GetGroupName(Guid boardId) => $"board:{boardId:D}";

    private static BoardStateDto ToBoardDto(Board board)
    {
        return new BoardStateDto(
            BoardId: board.Id.Value,
            Name: board.Name,
            Status: board.Status,
            MinimumApprovedTilesToStart: board.MinimumApprovedTilesToStart,
            Tiles: board.Tiles.Select(t => new TileDto(t.Id, t.Text, t.IsApproved, t.IsConfirmed, t.Status, t.CreatedByUserId)).ToArray(),
            Players: board.Players.Select(p => new PlayerDto(p.Id, p.DisplayName, p.GridTileIds.ToArray(), p.Roles.ToArray(), p.SilencedUntilUtc)).ToArray());
    }
}

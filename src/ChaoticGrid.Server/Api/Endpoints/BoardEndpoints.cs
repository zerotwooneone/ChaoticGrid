using ChaoticGrid.Server.Api.Dtos;
using ChaoticGrid.Server.Domain.Aggregates.BoardAggregate;
using ChaoticGrid.Server.Domain.Aggregates.IdentityAggregate;
using ChaoticGrid.Server.Domain.Enums;
using ChaoticGrid.Server.Domain.Interfaces;
using ChaoticGrid.Server.Domain.Services;
using ChaoticGrid.Server.Infrastructure.Hubs;
using ChaoticGrid.Server.Infrastructure.Persistence;
using ChaoticGrid.Server.Infrastructure.Security;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChaoticGrid.Server.Api.Endpoints;

public static class BoardEndpoints
{
    public static IEndpointRouteBuilder MapBoardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/boards");

        group.MapPost("/", CreateBoard).RequireAuthorization();
        group.MapPost("/{boardId:guid}/join", JoinBoard);
        group.MapPost("/{boardId:guid}/invite", CreateInvite).RequireAuthorization();
        group.MapPost("/join", JoinByInvite).RequireAuthorization();
        group.MapPost("/{boardId:guid}/start", StartBoard).RequireAuthorization();
        group.MapGet("/{boardId:guid}", GetBoardState);

        return endpoints;
    }

    private static async Task<Results<Ok<BoardStateDto>, ForbidHttpResult, BadRequest>> CreateBoard(
        CreateBoardRequest request,
        ClaimsPrincipal user,
        BoardCreationService boardCreation,
        IBoardRepository repo,
        CancellationToken ct)
    {
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub"), out var userId) || userId == Guid.Empty)
        {
            return TypedResults.Forbid();
        }

        var nickname = user.FindFirstValue("nickname") ?? "Host";

        Board board;
        try
        {
            board = await boardCreation.CreateBoard(userId, request.Name, nickname, ct);
        }
        catch
        {
            return TypedResults.BadRequest();
        }

        await repo.AddAsync(board, ct);
        return TypedResults.Ok(ToDto(board));
    }

    private static async Task<Results<Ok<BoardStateDto>, NotFound>> JoinBoard(Guid boardId, JoinBoardRequest request, IBoardRepository repo, CancellationToken ct)
    {
        var board = await repo.GetByIdAsync(new BoardId(boardId), ct);
        if (board is null)
        {
            return TypedResults.NotFound();
        }

        board.Join(new UserId(request.UserId), request.DisplayName, isHost: request.IsHost, seed: request.Seed);
        await repo.UpdateAsync(board, ct);

        return TypedResults.Ok(ToDto(board));
    }

    private static async Task<Results<Ok<BoardStateDto>, NotFound>> GetBoardState(Guid boardId, IBoardRepository repo, CancellationToken ct)
    {
        var board = await repo.GetByIdAsync(new BoardId(boardId), ct);
        return board is null ? TypedResults.NotFound() : TypedResults.Ok(ToDto(board));
    }

    private static async Task<Results<Ok<BoardStateDto>, NotFound, ForbidHttpResult, BadRequest>> StartBoard(
        Guid boardId,
        ClaimsPrincipal user,
        AppDbContext db,
        IHubContext<GameHub, IGameClient> hub,
        CancellationToken ct)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return TypedResults.Forbid();
        }

        if (!HasBoardPermission(db, userId, boardId, BoardPermission.ModifyBoardSettings, ct))
        {
            return TypedResults.Forbid();
        }

        var board = await db.Boards.FirstOrDefaultAsync(b => b.Id == new BoardId(boardId), ct);
        if (board is null)
        {
            return TypedResults.NotFound();
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        try
        {
            board.Start(seed: null);
            db.Boards.Update(board);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            return TypedResults.BadRequest();
        }

        var dto = ToDto(board);
        await hub.Clients.Group($"board:{boardId:D}").GameStarted(dto);
        await hub.Clients.Group($"board:{boardId:D}").BoardStateUpdated(dto);
        return TypedResults.Ok(dto);
    }

    private static async Task<Results<Ok<CreateInviteResponse>, NotFound, ForbidHttpResult, BadRequest>> CreateInvite(
        Guid boardId,
        CreateInviteRequest request,
        ClaimsPrincipal user,
        IBoardRepository repo,
        InviteService invites,
        CancellationToken ct)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return TypedResults.Forbid();
        }

        var boardForAuth = await repo.GetByIdAsync(new BoardId(boardId), ct);
        if (boardForAuth is null)
        {
            return TypedResults.NotFound();
        }

        if (!HasBoardPermission(boardForAuth, userId, BoardPermission.ManageBoardRoles))
        {
            return TypedResults.Forbid();
        }

        var board = await repo.GetByIdAsync(new BoardId(boardId), ct);
        if (board is null)
        {
            return TypedResults.NotFound();
        }

        if (request.ExpiresInMinutes <= 0)
        {
            return TypedResults.BadRequest();
        }

        var expiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(request.ExpiresInMinutes);
        var token = invites.CreateInviteToken(board.Id.Value, request.RoleId, expiresAtUtc);
        return TypedResults.Ok(new CreateInviteResponse(token, expiresAtUtc));
    }

    private static async Task<Results<Ok<BoardStateDto>, NotFound, ForbidHttpResult, BadRequest>> JoinByInvite(
        JoinByInviteRequest request,
        ClaimsPrincipal user,
        IBoardRepository repo,
        InviteService invites,
        CancellationToken ct)
    {
        if (!invites.TryValidate(request.Token, out var payload))
        {
            return TypedResults.BadRequest();
        }

        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub"), out var userId))
        {
            return TypedResults.Forbid();
        }

        var nickname = user.FindFirstValue("nickname") ?? "Player";

        var board = await repo.GetByIdAsync(new BoardId(payload.BoardId), ct);
        if (board is null)
        {
            return TypedResults.NotFound();
        }

        // Invite acceptance joins the game as the authenticated user.
        board.Join(new UserId(userId), nickname, isHost: false, seed: null);
        await repo.UpdateAsync(board, ct);

        return TypedResults.Ok(ToDto(board));
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out UserId userId)
    {
        userId = default;
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(raw, out var parsed) && parsed != Guid.Empty && (userId = new UserId(parsed)) != default;
    }

    private static bool HasBoardPermission(Board board, UserId userId, BoardPermission required)
    {
        var ctx = board.GetMyContext(userId);
        return ctx.EffectivePermissions.HasFlag(required);
    }

    private static bool HasBoardPermission(AppDbContext db, UserId userId, Guid boardId, BoardPermission required, CancellationToken ct)
    {
        var board = db.Boards.FirstOrDefault(b => b.Id == new BoardId(boardId));
        if (board is null)
        {
            return false;
        }

        return HasBoardPermission(board, userId, required);
    }

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

    public sealed record JoinBoardRequest(Guid UserId, string DisplayName, bool IsHost, int? Seed);
}

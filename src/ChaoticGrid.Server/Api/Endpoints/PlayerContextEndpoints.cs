using ChaoticGrid.Server.Api.Dtos;
using ChaoticGrid.Server.Domain.Aggregates.BoardAggregate;
using ChaoticGrid.Server.Domain.Aggregates.IdentityAggregate;
using ChaoticGrid.Server.Domain.Enums;
using ChaoticGrid.Server.Domain.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Security.Claims;

namespace ChaoticGrid.Server.Api.Endpoints;

public static class PlayerContextEndpoints
{
    public static IEndpointRouteBuilder MapPlayerContextEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/boards").RequireAuthorization();

        group.MapGet("/{boardId:guid}/my-context", GetMyContext);
        group.MapPut("/{boardId:guid}/players/me/permissions", UpdateMyPermissions);

        return endpoints;
    }

    private static async Task<Results<Ok<PlayerContextDto>, NotFound, ForbidHttpResult, BadRequest>> GetMyContext(
        Guid boardId,
        ClaimsPrincipal user,
        IBoardRepository boards,
        CancellationToken ct)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return TypedResults.Forbid();
        }

        var board = await boards.GetByIdAsync(new BoardId(boardId), ct);
        if (board is null)
        {
            return TypedResults.NotFound();
        }

        try
        {
            var ctx = board.GetMyContext(userId);
            return TypedResults.Ok(ToDto(ctx));
        }
        catch
        {
            return TypedResults.BadRequest();
        }
    }

    private static async Task<Results<Ok<PlayerContextDto>, NotFound, ForbidHttpResult, BadRequest>> UpdateMyPermissions(
        Guid boardId,
        UpdatePermissionOverrideRequest request,
        ClaimsPrincipal user,
        IBoardRepository boards,
        CancellationToken ct)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return TypedResults.Forbid();
        }

        var board = await boards.GetByIdAsync(new BoardId(boardId), ct);
        if (board is null)
        {
            return TypedResults.NotFound();
        }

        try
        {
            var current = board.GetMyContext(userId);
            if (!current.EffectivePermissions.HasFlag(BoardPermission.ModifyOwnPermissions))
            {
                return TypedResults.Forbid();
            }

            var player = board.Players.First(p => p.OwnerUserId == userId);
            player.SetPermissionOverrides(
                allowOverrides: (BoardPermission)request.AllowOverrideMask,
                denyOverrides: (BoardPermission)request.DenyOverrideMask);

            await boards.UpdateAsync(board, ct);

            var updated = board.GetMyContext(userId);
            return TypedResults.Ok(ToDto(updated));
        }
        catch
        {
            return TypedResults.BadRequest();
        }
    }

    private static PlayerContextDto ToDto(Domain.ValueObjects.PlayerContext ctx)
    {
        return new PlayerContextDto(
            RoleId: ctx.RoleId,
            RoleName: ctx.RoleName,
            RolePermissions: (int)ctx.RolePermissions,
            AllowOverrides: (int)ctx.AllowOverrides,
            DenyOverrides: (int)ctx.DenyOverrides,
            EffectivePermissions: (int)ctx.EffectivePermissions);
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out UserId userId)
    {
        userId = default;
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(raw, out var parsed) && parsed != Guid.Empty && (userId = new UserId(parsed)) != default;
    }
}

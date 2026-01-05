using ChaoticGrid.Server.Api.Dtos;
using ChaoticGrid.Server.Domain.Aggregates.IdentityAggregate;
using ChaoticGrid.Server.Domain.Enums;
using ChaoticGrid.Server.Domain.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Security.Claims;

namespace ChaoticGrid.Server.Api.Endpoints;

public static class MeEndpoints
{
    public static IEndpointRouteBuilder MapMeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/me").RequireAuthorization();

        group.MapGet("/system-context", GetMySystemContext);

        group.MapGet("/role-templates", GetMyRoleTemplates);
        group.MapPost("/role-templates", CreateRoleTemplate);
        group.MapPut("/role-templates/{templateId:guid}", UpdateRoleTemplate);
        group.MapDelete("/role-templates/{templateId:guid}", DeleteRoleTemplate);

        return endpoints;
    }

    private static async Task<Results<Ok<MySystemContextDto>, ForbidHttpResult>> GetMySystemContext(
        ClaimsPrincipal user,
        IUserRepository users,
        CancellationToken ct)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return TypedResults.Forbid();
        }

        var u = await users.GetByIdAsync(userId, ct);
        if (u is null)
        {
            return TypedResults.Forbid();
        }

        var perms = u.GetEffectivePermissions();
        return TypedResults.Ok(new MySystemContextDto(u.Nickname, (int)perms));
    }

    private static async Task<Results<Ok<RoleTemplateDto[]>, ForbidHttpResult>> GetMyRoleTemplates(
        ClaimsPrincipal user,
        IRoleTemplateRepository templates,
        CancellationToken ct)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return TypedResults.Forbid();
        }

        var list = await templates.GetByOwnerUserIdAsync(userId, ct);
        var dto = list.Select(t => new RoleTemplateDto(t.Id, t.Name, (int)t.DefaultBoardPermissions)).ToArray();
        return TypedResults.Ok(dto);
    }

    private static async Task<Results<Created<RoleTemplateDto>, ForbidHttpResult>> CreateRoleTemplate(
        CreateRoleTemplateRequest request,
        ClaimsPrincipal user,
        IRoleTemplateRepository templates,
        CancellationToken ct)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return TypedResults.Forbid();
        }

        if (!HasPermission(user, SystemPermission.CreateBoard))
        {
            return TypedResults.Forbid();
        }

        var template = new RoleTemplate(Guid.NewGuid(), userId, request.Name, (BoardPermission)request.DefaultBoardPermissions);
        await templates.AddAsync(template, ct);
        return TypedResults.Created($"/api/me/role-templates/{template.Id:D}", new RoleTemplateDto(template.Id, template.Name, (int)template.DefaultBoardPermissions));
    }

    private static async Task<Results<Ok<RoleTemplateDto>, NotFound, ForbidHttpResult>> UpdateRoleTemplate(
        Guid templateId,
        UpdateRoleTemplateRequest request,
        ClaimsPrincipal user,
        IRoleTemplateRepository templates,
        CancellationToken ct)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return TypedResults.Forbid();
        }

        if (!HasPermission(user, SystemPermission.CreateBoard))
        {
            return TypedResults.Forbid();
        }

        var template = await templates.GetByIdAsync(templateId, ct);
        if (template is null)
        {
            return TypedResults.NotFound();
        }

        if (template.OwnerUserId != userId)
        {
            return TypedResults.Forbid();
        }

        template = new RoleTemplate(template.Id, template.OwnerUserId, request.Name, (BoardPermission)request.DefaultBoardPermissions);
        await templates.UpdateAsync(template, ct);

        return TypedResults.Ok(new RoleTemplateDto(template.Id, template.Name, (int)template.DefaultBoardPermissions));
    }

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult>> DeleteRoleTemplate(
        Guid templateId,
        ClaimsPrincipal user,
        IRoleTemplateRepository templates,
        CancellationToken ct)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return TypedResults.Forbid();
        }

        if (!HasPermission(user, SystemPermission.CreateBoard))
        {
            return TypedResults.Forbid();
        }

        var template = await templates.GetByIdAsync(templateId, ct);
        if (template is null)
        {
            return TypedResults.NotFound();
        }

        if (template.OwnerUserId != userId)
        {
            return TypedResults.Forbid();
        }

        await templates.DeleteAsync(template, ct);
        return TypedResults.NoContent();
    }

    private static bool HasPermission(ClaimsPrincipal user, SystemPermission required)
    {
        var raw = user.FindFirstValue("x-permissions");
        if (!long.TryParse(raw, out var perms))
        {
            return false;
        }

        return (((SystemPermission)perms) & required) == required;
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
    {
        userId = default;
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(raw, out userId) && userId != Guid.Empty;
    }
}

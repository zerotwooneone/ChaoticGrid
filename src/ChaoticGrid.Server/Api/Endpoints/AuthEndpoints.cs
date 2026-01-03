using ChaoticGrid.Server.Api.Dtos;
using ChaoticGrid.Server.Domain.Interfaces;
using ChaoticGrid.Server.Infrastructure.Security;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ChaoticGrid.Server.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth");

        group.MapGet("/status", GetStatus);
        group.MapPost("/setup", Setup);

        return endpoints;
    }

    private static async Task<Ok<AuthStatusResponse>> GetStatus(IUserRepository users, CancellationToken ct)
    {
        var count = await users.CountAsync(ct);
        return TypedResults.Ok(new AuthStatusResponse(IsSetupRequired: count == 0));
    }

    private static async Task<Results<Ok<SetupResponse>, BadRequest>> Setup(
        SetupRequest request,
        InitialSetupService initialSetup,
        JwtTokenGenerator jwtTokenGenerator,
        CancellationToken ct)
    {
        var jwt = await initialSetup.TryClaimSetup(request.Token, request.Nickname, jwtTokenGenerator, ct);
        if (jwt is null)
        {
            return TypedResults.BadRequest();
        }

        return TypedResults.Ok(new SetupResponse(jwt));
    }
}

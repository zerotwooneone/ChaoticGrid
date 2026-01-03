namespace ChaoticGrid.Server.Api.Dtos;

public sealed record SetupRequest(string Token, string Nickname);

public sealed record SetupResponse(string Jwt);

public sealed record AuthStatusResponse(bool IsSetupRequired);

public sealed record CreateInviteRequest(Guid RoleId, int ExpiresInMinutes);

public sealed record CreateInviteResponse(string Token, DateTimeOffset ExpiresAtUtc);

public sealed record JoinByInviteRequest(string Token);

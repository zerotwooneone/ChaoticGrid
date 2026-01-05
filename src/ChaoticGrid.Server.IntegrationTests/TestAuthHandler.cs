using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChaoticGrid.Server.IntegrationTests;

public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("x-test-user", out var userHeader))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing x-test-user header."));
        }

        var raw = userHeader.ToString();
        if (!Guid.TryParse(raw, out var userId) || userId == Guid.Empty)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid x-test-user header."));
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("sub", userId.ToString())
        };

        if (Request.Headers.TryGetValue("x-test-permissions", out var permsHeader)
            && long.TryParse(permsHeader.ToString(), out var perms))
        {
            claims.Add(new Claim("x-permissions", perms.ToString()));
        }

        var identity = new ClaimsIdentity(
            claims,
            authenticationType: Scheme.Name);

        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

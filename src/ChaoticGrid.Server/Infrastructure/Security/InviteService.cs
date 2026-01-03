using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;

namespace ChaoticGrid.Server.Infrastructure.Security;

public sealed class InviteService(IConfiguration configuration)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string CreateInviteToken(Guid boardId, Guid roleId, DateTimeOffset expiresAtUtc)
    {
        var payload = new InvitePayload(boardId, roleId, expiresAtUtc);
        var json = JsonSerializer.Serialize(payload, JsonOptions);

        var payloadBytes = Encoding.UTF8.GetBytes(json);
        var payloadB64 = WebEncoders.Base64UrlEncode(payloadBytes);

        var sigBytes = Sign(payloadB64);
        var sigB64 = WebEncoders.Base64UrlEncode(sigBytes);

        return $"{payloadB64}.{sigB64}";
    }

    public bool TryValidate(string token, out InvitePayload payload)
    {
        payload = default;

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var parts = token.Split('.', 2);
        if (parts.Length != 2)
        {
            return false;
        }

        var payloadB64 = parts[0];
        var sigB64 = parts[1];

        byte[] providedSig;
        try
        {
            providedSig = WebEncoders.Base64UrlDecode(sigB64);
        }
        catch
        {
            return false;
        }

        var expectedSig = Sign(payloadB64);
        if (!CryptographicOperations.FixedTimeEquals(providedSig, expectedSig))
        {
            return false;
        }

        byte[] payloadBytes;
        try
        {
            payloadBytes = WebEncoders.Base64UrlDecode(payloadB64);
        }
        catch
        {
            return false;
        }

        InvitePayload? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<InvitePayload>(payloadBytes, JsonOptions);
        }
        catch
        {
            return false;
        }

        if (parsed is null)
        {
            return false;
        }

        if (parsed.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            return false;
        }

        payload = parsed;
        return true;
    }

    private byte[] Sign(string payloadB64)
    {
        var signingKey = configuration.GetSection("Invites")["SigningKey"]
            ?? "dev-only-invite-signing-key-change-me";

        var keyBytes = Encoding.UTF8.GetBytes(signingKey);
        using var hmac = new HMACSHA256(keyBytes);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadB64));
    }

    public sealed record InvitePayload(Guid BoardId, Guid RoleId, DateTimeOffset ExpiresAtUtc);
}

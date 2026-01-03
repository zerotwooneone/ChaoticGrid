namespace ChaoticGrid.Server.Domain.Entities;

public sealed record Tile(Guid Id, string Text, bool IsApproved)
{
    public static Tile Create(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Tile text cannot be empty.", nameof(text));
        }

        var normalized = text.Trim();
        return new Tile(Guid.NewGuid(), normalized, IsApproved: false);
    }

    public Tile Approve() => this with { IsApproved = true };

    public Tile Reject() => this with { IsApproved = false };
}

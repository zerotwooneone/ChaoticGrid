using ChaoticGrid.Server.Domain.Entities;

namespace ChaoticGrid.Server.Domain.Services;

public sealed class GridGeneratorService
{
    public IReadOnlyList<Guid> Generate(IReadOnlyList<Tile> pool, int? seed = null)
    {
        if (pool is null)
        {
            throw new ArgumentNullException(nameof(pool));
        }

        var approved = pool.Where(t => t.IsApproved).Select(t => t.Id).ToArray();
        if (approved.Length < 24)
        {
            throw new InvalidOperationException("At least 24 approved tiles are required to generate a 5x5 grid.");
        }

        var rng = seed is null ? Random.Shared : new Random(seed.Value);

        var shuffled = approved.ToArray();
        for (var i = shuffled.Length - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        var result = new List<Guid>(25);
        result.AddRange(shuffled.Take(24));
        result.Insert(12, Guid.Empty);

        return result;
    }
}

using ChaoticGrid.Server.Domain.Aggregates.BoardAggregate;
using ChaoticGrid.Server.Domain.ValueObjects;
using Xunit;

namespace ChaoticGrid.Server.Tests.Domain.ValueObjects;

public sealed class VoteTests
{
    [Fact]
    public void Create_ShouldThrow_WhenPlayerIdIsEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            Vote.Create(new PlayerId(Guid.Empty), Guid.NewGuid()));
    }

    [Fact]
    public void Create_ShouldThrow_WhenTileIdIsEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            Vote.Create(new PlayerId(Guid.NewGuid()), Guid.Empty));
    }

    [Fact]
    public void Create_ShouldCreateVote_WhenInputsAreValid()
    {
        var playerId = new PlayerId(Guid.NewGuid());
        var tileId = Guid.NewGuid();

        var vote = Vote.Create(playerId, tileId);

        Assert.Equal(playerId, vote.PlayerId);
        Assert.Equal(tileId, vote.TileId);
        Assert.NotEqual(default, vote.CastAt);
    }
}

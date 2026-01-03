using ChaoticGrid.Server.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace ChaoticGrid.Server.Tests.Domain;

public sealed class VoteTests
{
    [Fact]
    public void Create_should_set_cast_time_when_not_provided()
    {
        var playerId = Guid.NewGuid();
        var tileId = Guid.NewGuid();

        var vote = Vote.Create(playerId, tileId);

        vote.PlayerId.Should().Be(playerId);
        vote.TileId.Should().Be(tileId);
        vote.CastAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_should_throw_when_player_id_is_empty()
    {
        var act = () => Vote.Create(Guid.Empty, Guid.NewGuid());
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_should_throw_when_tile_id_is_empty()
    {
        var act = () => Vote.Create(Guid.NewGuid(), Guid.Empty);
        act.Should().Throw<ArgumentException>();
    }
}

using ChaoticGrid.Server.Domain.Aggregates.BoardAggregate;
using FluentAssertions;
using Xunit;

namespace ChaoticGrid.Server.Tests.Domain;

public sealed class BoardAggregateTests
{
    [Fact]
    public void Start_should_fail_when_not_enough_approved_tiles_exist()
    {
        var board = Board.Create("Test", minimumApprovedTilesToStart: 2);

        var hostId = Guid.NewGuid();
        board.AddPlayer(hostId, "Host", isHost: true);

        var tile1 = board.AddTileSuggestion(hostId, "Tile 1");
        board.ApproveTile(tile1.Id);

        var act = () => board.Start();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CastVote_should_confirm_tile_after_two_votes()
    {
        var board = Board.Create("Test", minimumApprovedTilesToStart: 1);

        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        board.AddPlayer(p1, "P1");
        board.AddPlayer(p2, "P2");

        var tile = board.AddTileSuggestion(p1, "Event");

        board.CastVote(p1, tile.Id, DateTime.UtcNow);
        board.Tiles.Single(t => t.Id == tile.Id).IsConfirmed.Should().BeFalse();

        board.CastVote(p2, tile.Id, DateTime.UtcNow);
        board.Tiles.Single(t => t.Id == tile.Id).IsConfirmed.Should().BeTrue();
    }

    [Fact]
    public void RejectTile_should_silence_proposer_until_future_time()
    {
        var board = Board.Create("Test", minimumApprovedTilesToStart: 1);

        var proposerId = Guid.NewGuid();
        board.AddPlayer(proposerId, "Proposer");

        var tile = board.AddTileSuggestion(proposerId, "Bad Tile");

        var now = new DateTime(2030, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromMinutes(5);

        board.RejectTile(tile.Id, now, duration);

        var proposer = board.Players.Single(p => p.Id == proposerId);
        proposer.SilencedUntilUtc.Should().Be(now.Add(duration));
        proposer.IsSilenced(now).Should().BeTrue();
    }

    [Fact]
    public void Silenced_player_cannot_cast_vote()
    {
        var board = Board.Create("Test", minimumApprovedTilesToStart: 1);

        var proposerId = Guid.NewGuid();
        var voterId = Guid.NewGuid();
        board.AddPlayer(proposerId, "Proposer");
        board.AddPlayer(voterId, "Voter");

        var tile = board.AddTileSuggestion(proposerId, "Some Event");

        var now = new DateTime(2030, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        board.Players.Single(p => p.Id == voterId).SilenceUntil(now.AddMinutes(10));

        var act = () => board.CastVote(voterId, tile.Id, now);
        act.Should().Throw<InvalidOperationException>().WithMessage("*silenced*");
    }
}

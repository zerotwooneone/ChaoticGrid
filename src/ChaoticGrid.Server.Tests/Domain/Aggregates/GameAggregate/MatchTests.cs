using ChaoticGrid.Server.Domain.Aggregates.GameAggregate;
using Xunit;

namespace ChaoticGrid.Server.Tests.Domain.Aggregates.GameAggregate;

public sealed class MatchTests
{
    [Fact]
    public void StartVote_ShouldReject_WhenVoteAlreadyInProgress()
    {
        var match = new Match();
        var proposerId = Guid.NewGuid();
        var tileId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        match.StartVote(proposerId, tileId, playerCount: 3, now, TimeSpan.FromMinutes(2));

        Assert.Throws<InvalidOperationException>(() =>
            match.StartVote(proposerId, Guid.NewGuid(), playerCount: 3, now, TimeSpan.FromMinutes(2)));
    }

    [Fact]
    public void AddVote_ShouldConfirm_WhenYesVotesReachThreshold()
    {
        var match = new Match();
        var proposerId = Guid.NewGuid();
        var tileId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        match.StartVote(proposerId, tileId, playerCount: 3, now, TimeSpan.FromMinutes(2));

        var r1 = match.AddVote(Guid.NewGuid(), isYes: true, now);
        Assert.Equal(Match.VoteResolutionType.None, r1.Type);

        var r2 = match.AddVote(Guid.NewGuid(), isYes: true, now);
        Assert.Equal(Match.VoteResolutionType.Confirmed, r2.Type);
        Assert.Equal(tileId, r2.TileId);
        Assert.Contains(tileId, match.CompletedTileIds);
    }

    [Fact]
    public void AddVote_ShouldRejectAndSilenceProposer_WhenNoVotesWinAfterThresholdReached()
    {
        var match = new Match();
        var proposerId = Guid.NewGuid();
        var tileId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        match.StartVote(proposerId, tileId, playerCount: 3, now, TimeSpan.FromMinutes(2));

        var r1 = match.AddVote(Guid.NewGuid(), isYes: false, now);
        Assert.Equal(Match.VoteResolutionType.None, r1.Type);

        var r2 = match.AddVote(Guid.NewGuid(), isYes: false, now);
        Assert.Equal(Match.VoteResolutionType.Rejected, r2.Type);
        Assert.Equal(tileId, r2.TileId);
        Assert.Equal(proposerId, r2.ProposerId);
        Assert.NotNull(r2.SilencedUntilUtc);
        Assert.True(match.IsSilenced(proposerId, now));
    }

    [Fact]
    public void ForceConfirm_ShouldAddCompletedTile()
    {
        var match = new Match();
        var tileId = Guid.NewGuid();

        var r = match.ForceConfirm(Guid.NewGuid(), tileId);

        Assert.Equal(Match.VoteResolutionType.Confirmed, r.Type);
        Assert.Contains(tileId, match.CompletedTileIds);
    }
}

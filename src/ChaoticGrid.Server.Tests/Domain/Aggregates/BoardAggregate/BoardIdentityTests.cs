using ChaoticGrid.Server.Domain.Aggregates.BoardAggregate;
using ChaoticGrid.Server.Domain.Aggregates.IdentityAggregate;
using Xunit;

namespace ChaoticGrid.Server.Tests.Domain.Aggregates.BoardAggregate;

public sealed class BoardIdentityTests
{
    [Fact]
    public void Join_WithSameUserTwice_ShouldReturnSamePlayer_AndNotDuplicatePlayer()
    {
        var board = Board.Create("Test Board");
        var userId = new UserId(Guid.NewGuid());

        var p1 = board.Join(userId, "Alice", isHost: false);
        var p2 = board.Join(userId, "Alice (ignored)", isHost: false);

        Assert.Equal(p1.Id, p2.Id);
        Assert.Single(board.Players);
        Assert.Equal(userId, board.Players[0].OwnerUserId);
    }

    [Fact]
    public void Join_WithDifferentUsers_ShouldCreateDistinctPlayers()
    {
        var board = Board.Create("Test Board");
        var userA = new UserId(Guid.NewGuid());
        var userB = new UserId(Guid.NewGuid());

        var pA = board.Join(userA, "A", isHost: false);
        var pB = board.Join(userB, "B", isHost: false);

        Assert.NotEqual(pA.Id, pB.Id);
        Assert.Equal(2, board.Players.Count);
    }

    [Fact]
    public void AddTileSuggestion_WhenUserHasNotJoined_ShouldThrow()
    {
        var board = Board.Create("Test Board");
        var userId = new UserId(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() =>
            board.AddTileSuggestion(userId, "My tile"));
    }

    [Fact]
    public void AddTileSuggestion_ShouldAttributeTileToResolvedPlayerId()
    {
        var board = Board.Create("Test Board");
        var userId = new UserId(Guid.NewGuid());
        var player = board.Join(userId, "Alice", isHost: false);

        var tile = board.AddTileSuggestion(userId, "My tile");

        Assert.Equal(player.Id, tile.CreatedByPlayerId);
    }
}

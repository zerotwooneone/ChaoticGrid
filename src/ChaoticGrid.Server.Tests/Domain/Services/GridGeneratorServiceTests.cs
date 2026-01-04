using ChaoticGrid.Server.Domain.Aggregates.BoardAggregate;
using ChaoticGrid.Server.Domain.Entities;
using ChaoticGrid.Server.Domain.Services;
using Xunit;

namespace ChaoticGrid.Server.Tests.Domain.Services;

public sealed class GridGeneratorServiceTests
{
    [Fact]
    public void Generate_ShouldCreate25Slots_WithFreeSpaceAtIndex12()
    {
        var svc = new GridGeneratorService();
        var boardId = new BoardId(Guid.NewGuid());

        var tiles = Enumerable.Range(0, 30)
            .Select(i => Tile.CreateSuggestion(boardId, $"Tile {i}", Guid.NewGuid()).Approve())
            .ToList();

        var grid = svc.Generate(tiles, seed: 123);

        Assert.Equal(25, grid.Count);
        Assert.Equal(Guid.Empty, grid[12]);

        var nonEmpty = grid.Where(id => id != Guid.Empty).ToArray();
        Assert.Equal(24, nonEmpty.Length);
        Assert.Equal(24, nonEmpty.Distinct().Count());
    }

    [Fact]
    public void Generate_WithSameSeed_ShouldBeDeterministic()
    {
        var svc = new GridGeneratorService();
        var boardId = new BoardId(Guid.NewGuid());

        var tiles = Enumerable.Range(0, 30)
            .Select(i => Tile.CreateSuggestion(boardId, $"Tile {i}", Guid.NewGuid()).Approve())
            .ToList();

        var a = svc.Generate(tiles, seed: 999);
        var b = svc.Generate(tiles, seed: 999);

        Assert.Equal(a, b);
    }

    [Fact]
    public void Generate_WithInsufficientApprovedTiles_ShouldThrow()
    {
        var svc = new GridGeneratorService();
        var boardId = new BoardId(Guid.NewGuid());

        var tiles = Enumerable.Range(0, 23)
            .Select(i => Tile.CreateSuggestion(boardId, $"Tile {i}", Guid.NewGuid()).Approve())
            .ToList();

        Assert.Throws<InvalidOperationException>(() => svc.Generate(tiles, seed: 1));
    }
}

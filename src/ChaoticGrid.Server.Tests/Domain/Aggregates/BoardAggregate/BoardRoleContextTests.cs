using ChaoticGrid.Server.Domain.Aggregates.BoardAggregate;
using ChaoticGrid.Server.Domain.Aggregates.IdentityAggregate;
using ChaoticGrid.Server.Domain.Enums;
using Xunit;

namespace ChaoticGrid.Server.Tests.Domain.Aggregates.BoardAggregate;

public sealed class BoardRoleContextTests
{
    [Fact]
    public void GetMyContext_WhenRoleAssigned_ShouldReturnRoleAndEffectivePermissions()
    {
        // Arrange
        var board = Board.Create("Test Board");
        var userId = new UserId(Guid.NewGuid());

        var role = board.CreateRole("Player", BoardPermission.SuggestTile);
        var player = board.Join(userId, "Alice");
        board.AssignRole(userId, role.Id);
        player.SetPermissionOverrides(allowOverrides: BoardPermission.CastVote, denyOverrides: BoardPermission.None);

        // Act
        var ctx = board.GetMyContext(userId);

        // Assert
        Assert.NotNull(ctx);
        Assert.Equal(role.Id, ctx!.RoleId);
        Assert.Equal(role.Name, ctx.RoleName);
        Assert.True(ctx.RolePermissions.HasFlag(BoardPermission.SuggestTile));
        Assert.True(ctx.AllowOverrides.HasFlag(BoardPermission.CastVote));
        Assert.True(ctx.EffectivePermissions.HasFlag(BoardPermission.SuggestTile));
        Assert.True(ctx.EffectivePermissions.HasFlag(BoardPermission.CastVote));
    }

    [Fact]
    public void GetMyContext_WhenNoRoleAssigned_ShouldReturnOverridesAsEffectivePermissions()
    {
        // Arrange
        var board = Board.Create("Test Board");
        var userId = new UserId(Guid.NewGuid());
        var player = board.Join(userId, "Alice");
        player.SetPermissionOverrides(allowOverrides: BoardPermission.CastVote, denyOverrides: BoardPermission.None);

        // Act
        var ctx = board.GetMyContext(userId);

        // Assert
        Assert.NotNull(ctx);
        Assert.Null(ctx!.RoleId);
        Assert.Null(ctx.RoleName);
        Assert.Equal(BoardPermission.None, ctx.RolePermissions);
        Assert.Equal(BoardPermission.CastVote, ctx.AllowOverrides);
        Assert.Equal(BoardPermission.None, ctx.DenyOverrides);
        Assert.Equal(BoardPermission.CastVote, ctx.EffectivePermissions);
    }
}

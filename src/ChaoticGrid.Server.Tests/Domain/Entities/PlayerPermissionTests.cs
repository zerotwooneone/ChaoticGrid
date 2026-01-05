using ChaoticGrid.Server.Domain.Aggregates.IdentityAggregate;
using ChaoticGrid.Server.Domain.Aggregates.BoardAggregate;
using ChaoticGrid.Server.Domain.Entities;
using ChaoticGrid.Server.Domain.Enums;
using Xunit;

namespace ChaoticGrid.Server.Tests.Domain.Entities;

public sealed class PlayerPermissionTests
{
    [Fact]
    public void EffectivePermissions_ShouldCombineRolePermissionsAndAllowOverrides()
    {
        // Arrange
        var role = BoardRole.Create("Player", BoardPermission.SuggestTile);
        var player = Player.Create(PlayerId.New(), new UserId(Guid.NewGuid()), "Alice");

        // Act
        player.SetPermissionOverrides(allowOverrides: BoardPermission.CastVote, denyOverrides: BoardPermission.None);
        var effective = player.GetEffectivePermissions(role.DefaultPermissions);

        // Assert
        Assert.True(effective.HasFlag(BoardPermission.SuggestTile));
        Assert.True(effective.HasFlag(BoardPermission.CastVote));
    }

    [Fact]
    public void EffectivePermissions_ShouldAllowMaskingRolePermissionsViaDenyOverrides()
    {
        // Arrange
        var role = BoardRole.Create("Moderator", BoardPermission.SuggestTile | BoardPermission.ApproveTile);
        var player = Player.Create(PlayerId.New(), new UserId(Guid.NewGuid()), "Alice");

        // Act
        player.SetPermissionOverrides(allowOverrides: BoardPermission.None, denyOverrides: BoardPermission.ApproveTile);
        var effective = player.GetEffectivePermissions(role.DefaultPermissions);

        // Assert
        Assert.True(effective.HasFlag(BoardPermission.SuggestTile));
        Assert.False(effective.HasFlag(BoardPermission.ApproveTile));
    }

    [Fact]
    public void SetPermissionOverrides_ShouldNotMutateRolePermissions()
    {
        // Arrange
        var role = BoardRole.Create("Player", BoardPermission.SuggestTile);
        var player = Player.Create(PlayerId.New(), new UserId(Guid.NewGuid()), "Alice");

        // Act
        player.SetPermissionOverrides(allowOverrides: BoardPermission.CastVote, denyOverrides: BoardPermission.None);

        // Assert
        Assert.Equal(BoardPermission.SuggestTile, role.DefaultPermissions);
    }
}

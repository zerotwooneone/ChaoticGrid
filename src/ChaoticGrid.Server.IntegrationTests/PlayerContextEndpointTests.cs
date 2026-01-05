using System.Net;
using System.Net.Http.Json;
using ChaoticGrid.Server.Domain.Aggregates.BoardAggregate;
using ChaoticGrid.Server.Domain.Aggregates.IdentityAggregate;
using ChaoticGrid.Server.Domain.Enums;
using ChaoticGrid.Server.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ChaoticGrid.Server.IntegrationTests;

public sealed class PlayerContextEndpointTests : IClassFixture<TestAppFactory>
{
    private readonly TestAppFactory _factory;
    private readonly HttpClient _client;

    public PlayerContextEndpointTests(TestAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetMyContext_ShouldReturnOk_WhenUserIsJoined()
    {
        // Arrange
        var creatorUserId = Guid.NewGuid();
        using var createReq = new HttpRequestMessage(HttpMethod.Post, "/boards")
        {
            Content = JsonContent.Create(new { name = "Test" })
        };
        createReq.Headers.Add("x-test-user", creatorUserId.ToString());

        var createResp = await _client.SendAsync(createReq);
        createResp.EnsureSuccessStatusCode();

        var created = await createResp.Content.ReadFromJsonAsync<BoardStateDto>();
        Assert.NotNull(created);

        var userId = Guid.NewGuid();
        var joinResp = await _client.PostAsJsonAsync($"/boards/{created!.boardId}/join", new
        {
            userId,
            displayName = "P1",
            isHost = false,
            seed = (int?)null
        });
        joinResp.EnsureSuccessStatusCode();

        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/boards/{created.boardId}/my-context");
        req.Headers.Add("x-test-user", userId.ToString());

        // Act
        var resp = await _client.SendAsync(req);

        // Assert
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<PlayerContextDto>();
        Assert.NotNull(dto);
        Assert.Equal(0, dto!.rolePermissions);
        Assert.Equal(0, dto.allowOverrides);
        Assert.Equal(0, dto.denyOverrides);
        Assert.Equal(0, dto.effectivePermissions);
    }

    [Fact]
    public async Task UpdateMyPermissions_ShouldForbid_WhenModifyOwnPermissionsNotEffective()
    {
        // Arrange
        var (boardId, userId) = await CreateBoardAndJoin();

        using var req = new HttpRequestMessage(HttpMethod.Put, $"/api/boards/{boardId}/players/me/permissions")
        {
            Content = JsonContent.Create(new { allowOverrideMask = (int)BoardPermission.CastVote, denyOverrideMask = (int)BoardPermission.None })
        };
        req.Headers.Add("x-test-user", userId.ToString());

        // Act
        var resp = await _client.SendAsync(req);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateMyPermissions_ShouldUpdateOverrides_WhenModifyOwnPermissionsEffective()
    {
        // Arrange
        var (boardId, userId) = await CreateBoardAndJoin();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var board = await db.Boards.FirstAsync(b => b.Id == new BoardId(Guid.Parse(boardId)));

            var player = board.Players.First(p => p.OwnerUserId == new UserId(userId));
            player.SetPermissionOverrides(allowOverrides: BoardPermission.ModifyOwnPermissions, denyOverrides: BoardPermission.None);

            db.Boards.Update(board);
            await db.SaveChangesAsync();
        }

        using var req = new HttpRequestMessage(HttpMethod.Put, $"/api/boards/{boardId}/players/me/permissions")
        {
            Content = JsonContent.Create(new { allowOverrideMask = (int)(BoardPermission.ModifyOwnPermissions | BoardPermission.CastVote), denyOverrideMask = (int)BoardPermission.None })
        };
        req.Headers.Add("x-test-user", userId.ToString());

        // Act
        var resp = await _client.SendAsync(req);

        // Assert
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<PlayerContextDto>();
        Assert.NotNull(dto);
        Assert.True(((BoardPermission)dto!.effectivePermissions).HasFlag(BoardPermission.CastVote));
        Assert.True(((BoardPermission)dto.effectivePermissions).HasFlag(BoardPermission.ModifyOwnPermissions));
    }

    private async Task<(string BoardId, Guid UserId)> CreateBoardAndJoin()
    {
        var creatorUserId = Guid.NewGuid();
        using var createReq = new HttpRequestMessage(HttpMethod.Post, "/boards")
        {
            Content = JsonContent.Create(new { name = "Test" })
        };
        createReq.Headers.Add("x-test-user", creatorUserId.ToString());

        var createResp = await _client.SendAsync(createReq);
        createResp.EnsureSuccessStatusCode();

        var created = await createResp.Content.ReadFromJsonAsync<BoardStateDto>();
        Assert.NotNull(created);

        var userId = Guid.NewGuid();
        var joinResp = await _client.PostAsJsonAsync($"/boards/{created!.boardId}/join", new
        {
            userId,
            displayName = "P1",
            isHost = false,
            seed = (int?)null
        });
        joinResp.EnsureSuccessStatusCode();

        return (created.boardId, userId);
    }

    private sealed class BoardStateDto
    {
        public required string boardId { get; init; }
        public required string name { get; init; }
        public required int status { get; init; }
        public required int minimumApprovedTilesToStart { get; init; }
        public required object[] tiles { get; init; }
        public required object[] players { get; init; }
    }

    private sealed class PlayerContextDto
    {
        public string? roleId { get; init; }
        public string? roleName { get; init; }
        public required int rolePermissions { get; init; }
        public required int allowOverrides { get; init; }
        public required int denyOverrides { get; init; }
        public required int effectivePermissions { get; init; }
    }
}

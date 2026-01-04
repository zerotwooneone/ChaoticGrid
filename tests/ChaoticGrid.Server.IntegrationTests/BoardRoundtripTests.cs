using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace ChaoticGrid.Server.IntegrationTests;

public sealed class BoardRoundtripTests : IClassFixture<TestAppFactory>
{
    private readonly HttpClient _client;

    public BoardRoundtripTests(TestAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ShouldReturnOk()
    {
        var resp = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task CreateJoinGetBoard_ShouldRoundtrip()
    {
        var createResp = await _client.PostAsJsonAsync("/boards", new { name = "Test" });
        createResp.EnsureSuccessStatusCode();

        var created = await createResp.Content.ReadFromJsonAsync<BoardStateDto>();
        Assert.NotNull(created);
        Assert.Equal("Test", created!.name);
        Assert.False(string.IsNullOrWhiteSpace(created.boardId));

        var playerId = Guid.NewGuid().ToString();
        var joinResp = await _client.PostAsJsonAsync($"/boards/{created.boardId}/join", new
        {
            playerId,
            displayName = "P1",
            isHost = false,
            seed = (int?)null
        });
        joinResp.EnsureSuccessStatusCode();

        var joined = await joinResp.Content.ReadFromJsonAsync<BoardStateDto>();
        Assert.NotNull(joined);
        Assert.Contains(joined!.players, p => p.id == playerId);

        var getResp = await _client.GetAsync($"/boards/{created.boardId}");
        getResp.EnsureSuccessStatusCode();

        var fetched = await getResp.Content.ReadFromJsonAsync<BoardStateDto>();
        Assert.NotNull(fetched);
        Assert.Equal(created.boardId, fetched!.boardId);
        Assert.Contains(fetched.players, p => p.id == playerId);
    }

    private sealed class BoardStateDto
    {
        public required string boardId { get; init; }
        public required string name { get; init; }
        public required int status { get; init; }
        public required int minimumApprovedTilesToStart { get; init; }
        public required TileDto[] tiles { get; init; }
        public required PlayerDto[] players { get; init; }
    }

    private sealed class TileDto
    {
        public required string id { get; init; }
        public required string text { get; init; }
        public required bool isApproved { get; init; }
        public required bool isConfirmed { get; init; }
        public required int status { get; init; }
        public required string createdByUserId { get; init; }
    }

    private sealed class PlayerDto
    {
        public required string id { get; init; }
        public required string displayName { get; init; }
        public required string[] gridTileIds { get; init; }
        public required string[] roles { get; init; }
        public string? silencedUntilUtc { get; init; }
    }
}

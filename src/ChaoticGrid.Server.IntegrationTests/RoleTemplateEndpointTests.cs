using System.Net;
using System.Net.Http.Json;
using ChaoticGrid.Server.Domain.Enums;
using Xunit;

namespace ChaoticGrid.Server.IntegrationTests;

public sealed class RoleTemplateEndpointTests : IClassFixture<TestAppFactory>
{
    private readonly HttpClient _client;

    public RoleTemplateEndpointTests(TestAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateRoleTemplate_ShouldForbid_WhenMissingCreateBoardPermission()
    {
        // Arrange
        var userId = Guid.NewGuid();
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/me/role-templates")
        {
            Content = JsonContent.Create(new { name = "My Template", defaultBoardPermissions = 1 })
        };
        req.Headers.Add("x-test-user", userId.ToString());
        req.Headers.Add("x-test-permissions", ((long)SystemPermission.None).ToString());

        // Act
        var resp = await _client.SendAsync(req);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task CreateAndListRoleTemplates_ShouldRoundtrip_WhenHasCreateBoardPermission()
    {
        // Arrange
        var userId = Guid.NewGuid();
        using var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/me/role-templates")
        {
            Content = JsonContent.Create(new { name = "My Template", defaultBoardPermissions = 3 })
        };
        createReq.Headers.Add("x-test-user", userId.ToString());
        createReq.Headers.Add("x-test-permissions", ((long)SystemPermission.CreateBoard).ToString());

        var createResp = await _client.SendAsync(createReq);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);

        using var listReq = new HttpRequestMessage(HttpMethod.Get, "/api/me/role-templates");
        listReq.Headers.Add("x-test-user", userId.ToString());

        // Act
        var listResp = await _client.SendAsync(listReq);

        // Assert
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        var list = await listResp.Content.ReadFromJsonAsync<RoleTemplateDto[]>();
        Assert.NotNull(list);
        Assert.Contains(list!, t => t.name == "My Template");
    }

    private sealed class RoleTemplateDto
    {
        public required string id { get; init; }
        public required string name { get; init; }
        public required int defaultBoardPermissions { get; init; }
    }
}

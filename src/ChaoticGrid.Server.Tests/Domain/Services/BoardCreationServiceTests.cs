using ChaoticGrid.Server.Domain.Aggregates.IdentityAggregate;
using ChaoticGrid.Server.Domain.Enums;
using ChaoticGrid.Server.Domain.Interfaces;
using ChaoticGrid.Server.Domain.Services;
using Xunit;

namespace ChaoticGrid.Server.Tests.Domain.Services;

public sealed class BoardCreationServiceTests
{
    [Fact]
    public async Task CreateBoard_ShouldHydrateRolesFromTemplates_AndAssignHostHighestRole()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var repo = new FakeRoleTemplateRepository(new[]
        {
            new RoleTemplate(Guid.NewGuid(), ownerUserId, "Low", BoardPermission.SuggestTile),
            new RoleTemplate(Guid.NewGuid(), ownerUserId, "High", BoardPermission.SuggestTile | BoardPermission.ManageBoardRoles)
        });

        var svc = new BoardCreationService(repo);

        // Act
        var board = await svc.CreateBoard(ownerUserId, "Test", "Host");

        // Assert
        Assert.Equal(2, board.BoardRoles.Count);
        Assert.Contains(board.BoardRoles, r => r.Name == "Low");
        Assert.Contains(board.BoardRoles, r => r.Name == "High");

        Assert.Single(board.Players);
        var host = board.Players[0];
        Assert.True(host.IsHost);

        var ctx = board.GetMyContext(host.OwnerUserId);
        Assert.Equal("High", ctx.RoleName);
    }

    [Fact]
    public async Task CreateBoard_ShouldFallbackToSystemDefaults_WhenUserHasNoTemplates()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var repo = new FakeRoleTemplateRepository(Array.Empty<RoleTemplate>());
        var svc = new BoardCreationService(repo);

        // Act
        var board = await svc.CreateBoard(ownerUserId, "Test", "Host");

        // Assert
        Assert.Equal(3, board.BoardRoles.Count);
        Assert.Contains(board.BoardRoles, r => r.Name == "Observer");
        Assert.Contains(board.BoardRoles, r => r.Name == "Player");
        Assert.Contains(board.BoardRoles, r => r.Name == "Moderator");

        var host = Assert.Single(board.Players);
        var ctx = board.GetMyContext(host.OwnerUserId);
        Assert.Equal("Moderator", ctx.RoleName);
    }

    private sealed class FakeRoleTemplateRepository : IRoleTemplateRepository
    {
        private readonly List<RoleTemplate> _templates;

        public FakeRoleTemplateRepository(IEnumerable<RoleTemplate> templates)
        {
            _templates = templates.ToList();
        }

        public Task<IReadOnlyList<RoleTemplate>> GetByOwnerUserIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
        {
            var result = _templates.Where(t => t.OwnerUserId == ownerUserId).ToList();
            return Task.FromResult((IReadOnlyList<RoleTemplate>)result);
        }

        public Task AddAsync(RoleTemplate template, CancellationToken cancellationToken = default)
        {
            _templates.Add(template);
            return Task.CompletedTask;
        }

        public Task<RoleTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var result = _templates.FirstOrDefault(t => t.Id == id);
            return Task.FromResult(result);
        }

        public Task UpdateAsync(RoleTemplate template, CancellationToken cancellationToken = default)
        {
            var idx = _templates.FindIndex(t => t.Id == template.Id);
            if (idx >= 0)
            {
                _templates[idx] = template;
            }
            else
            {
                _templates.Add(template);
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(RoleTemplate template, CancellationToken cancellationToken = default)
        {
            _templates.RemoveAll(t => t.Id == template.Id);
            return Task.CompletedTask;
        }
    }
}

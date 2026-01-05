using ChaoticGrid.Server.Domain.Aggregates.BoardAggregate;
using ChaoticGrid.Server.Domain.Aggregates.IdentityAggregate;
using ChaoticGrid.Server.Domain.Entities;
using ChaoticGrid.Server.Domain.Enums;
using ChaoticGrid.Server.Domain.Interfaces;

namespace ChaoticGrid.Server.Domain.Services;

public sealed class BoardCreationService(IRoleTemplateRepository roleTemplates)
{
    public async Task<Board> CreateBoard(
        Guid ownerUserId,
        string boardName,
        string hostDisplayName,
        CancellationToken ct = default)
    {
        if (ownerUserId == Guid.Empty)
        {
            throw new ArgumentException("OwnerUserId cannot be empty.", nameof(ownerUserId));
        }

        var board = Board.Create(boardName);

        var templates = await roleTemplates.GetByOwnerUserIdAsync(ownerUserId, ct);
        if (templates.Count == 0)
        {
            templates = GetSystemDefaultTemplates(ownerUserId);
        }

        var boardRoles = new List<BoardRole>();
        foreach (var t in templates)
        {
            boardRoles.Add(board.CreateRole(t.Name, t.DefaultBoardPermissions));
        }

        var host = board.Join(new UserId(ownerUserId), hostDisplayName, isHost: true);

        var hostRole = boardRoles
            .OrderByDescending(r => (int)r.DefaultPermissions)
            .FirstOrDefault();

        if (hostRole is not null)
        {
            board.AssignRole(host.OwnerUserId, hostRole.Id);
        }

        return board;
    }

    private static List<RoleTemplate> GetSystemDefaultTemplates(Guid ownerUserId)
    {
        return new List<RoleTemplate>
        {
            new(Guid.NewGuid(), ownerUserId, "Observer", BoardPermission.None),
            new(Guid.NewGuid(), ownerUserId, "Player", BoardPermission.SuggestTile | BoardPermission.CastVote),
            new(Guid.NewGuid(), ownerUserId, "Moderator", (BoardPermission.SuggestTile | BoardPermission.CastVote) | BoardPermission.ApproveTile | BoardPermission.ManageBoardRoles)
        };
    }
}

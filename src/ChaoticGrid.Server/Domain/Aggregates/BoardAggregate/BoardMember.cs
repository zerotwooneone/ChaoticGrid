using ChaoticGrid.Server.Domain.Aggregates.IdentityAggregate;
using ChaoticGrid.Server.Domain.Shared;

namespace ChaoticGrid.Server.Domain.Aggregates.BoardAggregate;

public sealed class BoardMember : Entity<Guid>
{
    public Guid UserId { get; private set; }

    public BoardId BoardId { get; private set; }

    public Role Role { get; private set; }

    public BoardMember(Guid id, Guid userId, BoardId boardId, Role role)
        : base(id)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        UserId = userId;
        BoardId = boardId;
        Role = role ?? throw new ArgumentNullException(nameof(role));
    }
}

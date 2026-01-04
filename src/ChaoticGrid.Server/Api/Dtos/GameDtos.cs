using ChaoticGrid.Server.Domain.Aggregates.BoardAggregate;
using ChaoticGrid.Server.Domain.Enums;

namespace ChaoticGrid.Server.Api.Dtos;

public sealed record CreateBoardRequest(string Name);

public sealed record VoteRequest(Guid PlayerId, Guid TileId);

public sealed record CompletionVoteRequest(Guid TileId, bool IsYes);

public sealed record CompletionVoteStartedDto(Guid TileId, Guid ProposerId);

public sealed record TileConfirmedDto(Guid TileId);

public sealed record BoardStateDto(
    Guid BoardId,
    string Name,
    BoardStatus Status,
    int MinimumApprovedTilesToStart,
    IReadOnlyList<TileDto> Tiles,
    IReadOnlyList<PlayerDto> Players);

public sealed record TileDto(Guid Id, string Text, bool IsApproved, bool IsConfirmed, TileStatus Status, Guid CreatedByPlayerId);

public sealed record PlayerDto(Guid Id, string DisplayName, IReadOnlyList<Guid> GridTileIds, IReadOnlyList<string> Roles, DateTime? SilencedUntilUtc);

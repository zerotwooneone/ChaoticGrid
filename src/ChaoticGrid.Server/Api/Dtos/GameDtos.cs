using ChaoticGrid.Server.Domain.Aggregates.BoardAggregate;

namespace ChaoticGrid.Server.Api.Dtos;

public sealed record CreateBoardRequest(string Name);

public sealed record VoteRequest(Guid PlayerId, Guid TileId);

public sealed record BoardStateDto(
    Guid BoardId,
    string Name,
    BoardStatus Status,
    int MinimumApprovedTilesToStart,
    IReadOnlyList<TileDto> Tiles,
    IReadOnlyList<PlayerDto> Players);

public sealed record TileDto(Guid Id, string Text, bool IsApproved, bool IsConfirmed);

public sealed record PlayerDto(Guid Id, string DisplayName, IReadOnlyList<Guid> GridTileIds, IReadOnlyList<string> Roles, DateTime? SilencedUntilUtc);

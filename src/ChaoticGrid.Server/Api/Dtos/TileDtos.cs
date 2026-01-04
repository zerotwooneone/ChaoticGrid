using ChaoticGrid.Server.Domain.Enums;

namespace ChaoticGrid.Server.Api.Dtos;

public sealed record SuggestTileRequest(Guid BoardId, string Text);

public sealed record ModerateTileRequest(Guid BoardId, TileStatus Status);

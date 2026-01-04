using ChaoticGrid.Server.Api.Dtos;

namespace ChaoticGrid.Server.Infrastructure.Hubs;

public interface IGameClient
{
    Task BoardStateUpdated(BoardStateDto state);

    Task VoteCast(VoteRequest vote);

    Task TileSuggested(TileDto tile);

    Task TileModerated(TileDto tile);

    Task GameStarted(BoardStateDto state);

    Task VoteRequested(CompletionVoteStartedDto vote);

    Task TileConfirmed(TileConfirmedDto tile);
}

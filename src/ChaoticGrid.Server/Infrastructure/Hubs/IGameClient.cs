using ChaoticGrid.Server.Api.Dtos;

namespace ChaoticGrid.Server.Infrastructure.Hubs;

public interface IGameClient
{
    Task BoardStateUpdated(BoardStateDto state);

    Task VoteCast(VoteRequest vote);
}

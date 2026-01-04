using System.Collections.Concurrent;

namespace ChaoticGrid.Server.Domain.Aggregates.GameAggregate;

public sealed class Match
{
    private readonly object _gate = new();

    public ActiveVoteState? ActiveVote { get; private set; }

    public HashSet<Guid> CompletedTileIds { get; } = [];

    public ConcurrentDictionary<Guid, DateTime> SilencedUsers { get; } = new();

    public void StartVote(Guid proposerId, Guid tileId, int playerCount, DateTime utcNow, TimeSpan silenceDuration)
    {
        lock (_gate)
        {
            if (ActiveVote is not null)
            {
                throw new InvalidOperationException("A vote is already in progress.");
            }

            if (IsSilenced(proposerId, utcNow))
            {
                throw new InvalidOperationException("Proposer is silenced.");
            }

            if (CompletedTileIds.Contains(tileId))
            {
                throw new InvalidOperationException("Tile already completed.");
            }

            ActiveVote = new ActiveVoteState(proposerId, tileId, GetThreshold(playerCount), silenceDuration);
        }
    }

    public VoteResolution AddVote(Guid voterId, bool isYes, DateTime utcNow)
    {
        lock (_gate)
        {
            if (ActiveVote is null)
            {
                throw new InvalidOperationException("No vote is in progress.");
            }

            if (IsSilenced(voterId, utcNow))
            {
                throw new InvalidOperationException("Voter is silenced.");
            }

            if (!ActiveVote.Votes.Add(voterId, isYes))
            {
                throw new InvalidOperationException("Voter already voted.");
            }

            return ResolveVoteInternal(utcNow);
        }
    }

    public VoteResolution ForceConfirm(Guid proposerId, Guid tileId)
    {
        lock (_gate)
        {
            if (ActiveVote is not null)
            {
                throw new InvalidOperationException("A vote is already in progress.");
            }

            CompletedTileIds.Add(tileId);
            return VoteResolution.Confirmed(tileId);
        }
    }

    private VoteResolution ResolveVoteInternal(DateTime utcNow)
    {
        if (ActiveVote is null)
        {
            return VoteResolution.None;
        }

        var yesVotes = ActiveVote.Votes.YesCount;
        var noVotes = ActiveVote.Votes.NoCount;
        var total = yesVotes + noVotes;

        if (yesVotes >= ActiveVote.Threshold)
        {
            var tileId = ActiveVote.TileId;
            CompletedTileIds.Add(tileId);
            ActiveVote = null;
            return VoteResolution.Confirmed(tileId);
        }

        if (total >= ActiveVote.Threshold && noVotes > yesVotes)
        {
            var proposer = ActiveVote.ProposerId;
            var until = utcNow.Add(ActiveVote.SilenceDuration);
            SilencedUsers[proposer] = until;

            var tileId = ActiveVote.TileId;
            ActiveVote = null;
            return VoteResolution.Rejected(tileId, proposer, until);
        }

        return VoteResolution.None;
    }

    public bool IsSilenced(Guid userId, DateTime utcNow)
    {
        if (!SilencedUsers.TryGetValue(userId, out var until))
        {
            return false;
        }

        if (until <= utcNow)
        {
            SilencedUsers.TryRemove(userId, out _);
            return false;
        }

        return true;
    }

    private static int GetThreshold(int playerCount)
    {
        if (playerCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(playerCount));
        }

        return (playerCount / 2) + 1;
    }

    public sealed record ActiveVoteState(Guid ProposerId, Guid TileId, int Threshold, TimeSpan SilenceDuration)
    {
        public VoteSet Votes { get; } = new();
    }

    public sealed class VoteSet
    {
        private readonly Dictionary<Guid, bool> _votes = new();

        public int YesCount => _votes.Count(v => v.Value);

        public int NoCount => _votes.Count(v => !v.Value);

        public bool Add(Guid voterId, bool isYes) => _votes.TryAdd(voterId, isYes);
    }

    public sealed record VoteResolution(VoteResolutionType Type, Guid TileId, Guid? ProposerId, DateTime? SilencedUntilUtc)
    {
        public static readonly VoteResolution None = new(VoteResolutionType.None, Guid.Empty, null, null);

        public static VoteResolution Confirmed(Guid tileId) => new(VoteResolutionType.Confirmed, tileId, null, null);

        public static VoteResolution Rejected(Guid tileId, Guid proposerId, DateTime silencedUntilUtc) =>
            new(VoteResolutionType.Rejected, tileId, proposerId, silencedUntilUtc);
    }

    public enum VoteResolutionType
    {
        None = 0,
        Confirmed = 1,
        Rejected = 2
    }
}

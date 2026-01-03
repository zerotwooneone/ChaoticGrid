namespace ChaoticGrid.Server.Domain.Aggregates.BoardAggregate;

public readonly record struct BoardId(Guid Value)
{
    public static BoardId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}

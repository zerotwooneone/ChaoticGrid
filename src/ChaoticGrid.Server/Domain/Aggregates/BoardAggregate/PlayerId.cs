namespace ChaoticGrid.Server.Domain.Aggregates.BoardAggregate;

public readonly record struct PlayerId(Guid Value)
{
    public static PlayerId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}

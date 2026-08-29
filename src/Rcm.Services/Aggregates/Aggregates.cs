namespace Rcm.Services.Aggregates;

public sealed record class Aggregates
{
    public required AggregateEntry First { get; init; }
    public required AggregateEntry Min { get; init; }
    public required AggregateEntry Max { get; init; }
    public required AggregateEntry Last { get; init; }
}

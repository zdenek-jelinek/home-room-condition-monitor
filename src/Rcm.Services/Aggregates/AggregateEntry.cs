using System;

namespace Rcm.Services.Aggregates;

public sealed record class AggregateEntry
{
    public required DateTimeOffset Time { get; init; }
    public required decimal Value { get; init; }
}

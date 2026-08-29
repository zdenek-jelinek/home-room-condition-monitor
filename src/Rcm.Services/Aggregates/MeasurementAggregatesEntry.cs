using System;

namespace Rcm.Services.Aggregates;

public sealed record class MeasurementAggregatesEntry
{
    public required DateTimeOffset Time { get; init; }
    public required decimal Value { get; init; }
}

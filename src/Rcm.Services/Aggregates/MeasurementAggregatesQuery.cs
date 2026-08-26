using System;

namespace Rcm.Services.Aggregates;

public sealed record class MeasurementAggregatesQuery
{
    public required DateTimeOffset StartTime { get; init; }
    public required DateTimeOffset EndTime { get; init; }
    public required int PartitionCount { get; init; }
}

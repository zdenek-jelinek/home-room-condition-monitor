namespace Rcm.Services.Aggregates;

public sealed record class MeasurementDimensionAggregates
{
    public required MeasurementAggregatesEntry First { get; init; }
    public required MeasurementAggregatesEntry Min { get; init; }
    public required MeasurementAggregatesEntry Max { get; init; }
    public required MeasurementAggregatesEntry Last { get; init; }
}

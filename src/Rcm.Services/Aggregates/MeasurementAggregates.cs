namespace Rcm.Services.Aggregates;

public sealed record class MeasurementAggregates
{
    public required MeasurementDimensionAggregates Temperature { get; init; }
    public required MeasurementDimensionAggregates Pressure { get; init; }
    public required MeasurementDimensionAggregates Humidity { get; init; }
}

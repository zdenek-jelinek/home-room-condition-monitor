namespace Rcm.Services.Aggregates;

public sealed record class MeasurementAggregates
{
    public required Aggregates Temperature { get; init; }
    public required Aggregates Pressure { get; init; }
    public required Aggregates Humidity { get; init; }
}

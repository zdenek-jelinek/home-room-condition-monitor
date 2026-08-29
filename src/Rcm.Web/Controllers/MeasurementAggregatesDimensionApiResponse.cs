namespace Rcm.Web.Controllers;

public sealed class MeasurementAggregatesDimensionApiResponse
{
    public required MeasurementAggregatesEntryApiResponse First { get; init; }
    public required MeasurementAggregatesEntryApiResponse Min { get; init; }
    public required MeasurementAggregatesEntryApiResponse Max { get; init; }
    public required MeasurementAggregatesEntryApiResponse Last { get; init; }
}

namespace Rcm.Web.Controllers;

public class MeasurementAggregatesApiResponse
{
    public required MeasurementAggregatesDimensionApiResponse Temperature { get; init; }
    public required MeasurementAggregatesDimensionApiResponse Pressure { get; init; }
    public required MeasurementAggregatesDimensionApiResponse Humidity { get; init; }
}

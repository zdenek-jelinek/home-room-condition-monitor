namespace Rcm.Web.Controllers;

public class MeasurementAggregatesApiResponse
{
    public required AggregatesApiResponse Temperature { get; init; }
    public required AggregatesApiResponse Pressure { get; init; }
    public required AggregatesApiResponse Humidity { get; init; }
}

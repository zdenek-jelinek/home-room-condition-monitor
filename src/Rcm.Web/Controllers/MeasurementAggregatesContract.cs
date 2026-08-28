namespace Rcm.Web.Controllers;

public class MeasurementAggregatesContract // TODO Rename to ApiResponse (Zdenek Jelinek, 28. 8. 2026)
{
    public required AggregatesApiResponse Temperature { get; init; }
    public required AggregatesApiResponse Pressure { get; init; }
    public required AggregatesApiResponse Humidity { get; init; }
}

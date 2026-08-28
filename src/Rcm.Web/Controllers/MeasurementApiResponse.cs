using System;

namespace Rcm.Web.Controllers;

public sealed class MeasurementApiResponse
{
    public required DateTimeOffset Time { get; init; }
    public required decimal CelsiusTemperature { get; init; }
    public required decimal HpaPressure { get; init; }
    public required decimal RelativeHumidity { get; init; }
}

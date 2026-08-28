using System;

namespace Rcm.Web.Controllers;

public class MeasurementApiResponse
{
    public DateTimeOffset Time { get; set; }
    public decimal CelsiusTemperature { get; set; }
    public decimal HpaPressure { get; set; }
    public decimal RelativeHumidity { get; set; }

    public MeasurementApiResponse(
        DateTimeOffset time,
        decimal celsiusTemperature,
        decimal hpaPressure,
        decimal relativeHumidity)
    {
        Time = time;
        CelsiusTemperature = celsiusTemperature;
        HpaPressure = hpaPressure;
        RelativeHumidity = relativeHumidity;
    }
}

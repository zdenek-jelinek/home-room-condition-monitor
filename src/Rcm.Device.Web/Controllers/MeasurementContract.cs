using System;

namespace Rcm.Device.Web.Controllers;

public class MeasurementContract
{
    public DateTimeOffset Time { get; set; }
    public decimal CelsiusTemperature { get; set; }
    public decimal HpaPressure { get; set; }
    public decimal RelativeHumidity { get; set; }

    public MeasurementContract(
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
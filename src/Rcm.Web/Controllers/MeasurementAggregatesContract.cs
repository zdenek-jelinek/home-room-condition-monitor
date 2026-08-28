namespace Rcm.Web.Controllers;

public class MeasurementAggregatesContract
{
    public AggregatesApiResponse Temperature { get; }
    public AggregatesApiResponse Pressure { get; }
    public AggregatesApiResponse Humidity { get; }

    public MeasurementAggregatesContract(AggregatesApiResponse temperature, AggregatesApiResponse pressure, AggregatesApiResponse humidity)
    {
        Temperature = temperature;
        Pressure = pressure;
        Humidity = humidity;
    }
}

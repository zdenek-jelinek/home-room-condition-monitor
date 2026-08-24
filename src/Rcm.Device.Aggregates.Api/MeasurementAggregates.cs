namespace Rcm.Device.Aggregates.Api;

public class MeasurementAggregates
{
    public Aggregates Temperature { get; }
    public Aggregates Pressure { get; }
    public Aggregates Humidity { get; }

    public MeasurementAggregates(Aggregates temperature, Aggregates pressure, Aggregates humidity)
    {
        Temperature = temperature;
        Pressure = pressure;
        Humidity = humidity;
    }
}
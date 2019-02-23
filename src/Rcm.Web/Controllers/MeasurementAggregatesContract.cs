namespace Rcm.Web.Controllers
{
    public class MeasurementAggregatesContract
    {
        public AggregatesContract Temperature { get; }
        public AggregatesContract Pressure { get; }
        public AggregatesContract Humidity { get; }

        public MeasurementAggregatesContract(AggregatesContract temperature, AggregatesContract pressure, AggregatesContract humidity)
        {
            Temperature = temperature;
            Pressure = pressure;
            Humidity = humidity;
        }
    }
}
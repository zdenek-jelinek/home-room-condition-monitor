using System;

namespace Rcm.Backend.Persistence.Measurements
{
    public class Measurement
    {
        public DateTime Time { get; }
        public decimal Temperature { get; }
        public decimal Pressure { get; }
        public decimal Humidity { get; }

        public Measurement(DateTime time, decimal temperature, decimal pressure, decimal humidity)
        {
            if (time.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Measurements can only be created with UTC datetime");
            }

            Time = time;
            Temperature = temperature;
            Pressure = pressure;
            Humidity = humidity;
        }
    }
}
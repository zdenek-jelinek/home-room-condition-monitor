using System;
using System.Collections.Generic;
using System.Text;

namespace Rcm.Measurement.Api
{
    public class MeasurementEntry
    {
        public DateTimeOffset Time { get; }
        public decimal CelsiusTemperature { get; }
        public decimal RelativeHumidity { get; }
        public decimal HpaPressure { get; }

        public MeasurementEntry(DateTimeOffset time, decimal celsiusTemperature, decimal relativeHumidity, decimal hpaPressure)
        {
            Time = time;
            CelsiusTemperature = celsiusTemperature;
            RelativeHumidity = relativeHumidity;
            HpaPressure = hpaPressure;
        }
    }
}

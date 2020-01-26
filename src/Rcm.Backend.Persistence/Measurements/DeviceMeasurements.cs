using System.Collections.Generic;

namespace Rcm.Backend.Persistence.Measurements
{
    public class DeviceMeasurements
    {
        public string DeviceIdentifier { get; }
        public IEnumerable<Measurement> Measurements { get; }

        public DeviceMeasurements(string deviceIdentifier, IEnumerable<Measurement> measurements)
        {
            DeviceIdentifier = deviceIdentifier;
            Measurements = measurements;
        }
    }
}

using System.Collections.Generic;

namespace Rcm.Backend.Device.Api;

public class MeasurementsIngressModel
{
    public string? DeviceIdentifier { get; set; }
    public IEnumerable<MeasurementEntryIngressModel>? Measurements { get; set; }
}
using System;

namespace Rcm.Device.Connector.Configuration;

public interface ILatestUploadedMeasurementReader
{
    DateTimeOffset? GetLatestUploadedMeasurementTime();
}
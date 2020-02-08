using System;

namespace Rcm.Connector.Configuration
{
    public interface ILatestUploadedMeasurementReader
    {
        DateTimeOffset? GetLatestUploadedMeasurementTime();
    }
}
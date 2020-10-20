using System;

namespace Rcm.Device.Connector.Configuration
{
    public interface ILatestUploadedMeasurementWriter
    {
        void SetLatestMeasurementUploadTime(DateTimeOffset? time);
    }
}
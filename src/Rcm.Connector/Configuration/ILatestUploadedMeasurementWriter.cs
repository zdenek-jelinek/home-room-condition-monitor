using System;

namespace Rcm.Connector.Configuration
{
    public interface ILatestUploadedMeasurementWriter
    {
        void SetLatestMeasurementUploadTime(DateTimeOffset? time);
    }
}
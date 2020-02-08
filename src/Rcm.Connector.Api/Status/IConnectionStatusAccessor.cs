using System;

namespace Rcm.Connector.Api.Status
{
    public interface IConnectionStatusAccessor
    {
        bool IsConfigured { get; }
        DateTimeOffset? LastUploadedMeasurementTime { get; }
    }
}

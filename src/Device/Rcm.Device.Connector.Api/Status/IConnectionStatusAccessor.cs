using System;

namespace Rcm.Device.Connector.Api.Status;

public interface IConnectionStatusAccessor
{
    bool IsConfigured { get; }
    DateTimeOffset? LastUploadedMeasurementTime { get; }
}
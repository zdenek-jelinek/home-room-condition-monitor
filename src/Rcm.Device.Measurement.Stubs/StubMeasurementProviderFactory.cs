using System;
using Rcm.Common;
using Rcm.Device.Measurement.Api;

namespace Rcm.Device.Measurement.Stubs;

public class StubMeasurementProviderFactory : IMeasurementProviderFactory
{
    private readonly Lazy<IMeasurementProvider> _instance;

    public StubMeasurementProviderFactory(IClock clock)
    {
        _instance = new Lazy<IMeasurementProvider>(() => new StubMeasurementProvider(clock));
    }

    public IMeasurementProvider Create() => _instance.Value;
}
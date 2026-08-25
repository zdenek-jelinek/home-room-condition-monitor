using System;
using Rcm.Common;
using Rcm.Sensors.Abstractions;

namespace Rcm.Sensors.Fakes;

public class StubMeasurementProviderFactory : IMeasurementProviderFactory
{
    private readonly Lazy<IMeasurementProvider> _instance;

    public StubMeasurementProviderFactory(IClock clock)
    {
        _instance = new Lazy<IMeasurementProvider>(() => new StubMeasurementProvider(clock));
    }

    public IMeasurementProvider Create() => _instance.Value;
}
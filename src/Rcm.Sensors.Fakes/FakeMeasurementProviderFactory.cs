using System;
using Rcm.Common.Temporal;
using Rcm.Sensors.Abstractions;

namespace Rcm.Sensors.Fakes;

public class FakeMeasurementProviderFactory : IMeasurementProviderFactory
{
    private readonly Lazy<IMeasurementProvider> _instance;

    public FakeMeasurementProviderFactory(IClock clock)
    {
        _instance = new Lazy<IMeasurementProvider>(() => new FakeMeasurementProvider(clock));
    }

    public IMeasurementProvider Create() => _instance.Value;
}
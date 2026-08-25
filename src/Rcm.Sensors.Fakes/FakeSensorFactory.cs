using System;
using Rcm.Common.Temporal;
using Rcm.Sensors.Abstractions;

namespace Rcm.Sensors.Fakes;

public class FakeSensorFactory : ISensorFactory
{
    private readonly Lazy<ISensor> _instance;

    public FakeSensorFactory(IClock clock)
    {
        _instance = new Lazy<ISensor>(() => new FakeSensor(clock));
    }

    public ISensor Create() => _instance.Value;
}
using System;
using Rcm.Common.Temporal;
using Rcm.Sensors.Abstractions;

namespace Rcm.Sensors.Fakes;

public class FakeSensorFactory(IClock clock) : ISensorFactory
{
    private readonly Lazy<ISensor> _instance = new(() => new FakeSensor(clock));

    public ISensor Create() => _instance.Value;
}

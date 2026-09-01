using Rcm.Common.Temporal;
using Rcm.Sensors.Abstractions;

namespace Rcm.Sensors.Fakes;

public class FakeSensorFactory(IClock clock) : ISensorFactory
{
    private FakeSensor? _instance;

    public ISensor Create()
    {
        return _instance ??= new FakeSensor(clock);
    }
}

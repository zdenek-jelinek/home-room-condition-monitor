namespace Rcm.Sensors.Abstractions;

public interface IMeasurementProviderFactory
{
    ISensor Create();
}
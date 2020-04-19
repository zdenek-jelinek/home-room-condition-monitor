namespace Rcm.Bme280
{
    public interface II2cAccessConfiguration
    {
        string I2cBusAddress { get; }
        byte DeviceAddress { get; }
    }
}
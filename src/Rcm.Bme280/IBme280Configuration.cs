namespace Rcm.Bme280
{
    public interface IBme280Configuration
    {
        string I2cBusAddress { get; }
        byte DeviceAddress { get; }
    }
}
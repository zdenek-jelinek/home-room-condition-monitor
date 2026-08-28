using Microsoft.Extensions.Logging;

namespace Rcm.I2c;

public class I2cBusFactory(ILogger<I2cBus> logger)
{
    public I2cBus Open(string i2cBus)
    {
        return I2cBus.Open(logger, i2cBus);
    }
}

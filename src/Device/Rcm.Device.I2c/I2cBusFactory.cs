using Microsoft.Extensions.Logging;

namespace Rcm.Device.I2c
{
    public class I2cBusFactory
    {
        private readonly ILogger<I2cBus> _logger;

        public I2cBusFactory(ILogger<I2cBus> logger)
        {
            _logger = logger;
        }

        public I2cBus Open(string i2cBus)
        {
            return I2cBus.Open(_logger, i2cBus);
        }
    }
}

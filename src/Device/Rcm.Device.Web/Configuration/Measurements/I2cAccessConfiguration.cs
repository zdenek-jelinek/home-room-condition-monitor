using System;
using System.ComponentModel.DataAnnotations;
using Rcm.Device.Bme280;

namespace Rcm.Device.Web.Configuration.Measurements
{
    public class I2cAccessConfiguration : II2cAccessConfiguration
    {
        [Required(AllowEmptyStrings = false)]
        public string? BusAddress { get; set; }
        [Required]
        public byte? DeviceAddress { get; set; }

        byte II2cAccessConfiguration.DeviceAddress => DeviceAddress
            ?? throw new InvalidOperationException($"{nameof(DeviceAddress)} is unexpectedly null.");

        string II2cAccessConfiguration.I2cBusAddress => BusAddress
            ?? throw new InvalidOperationException($"{nameof(BusAddress)} is unexpectedly null.");
    }
}

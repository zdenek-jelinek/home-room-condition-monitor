using System;
using System.ComponentModel.DataAnnotations;
using Rcm.Bme280;

namespace Rcm.Web.Configuration.Measurements
{
    public class I2cAccessConfiguration : IBme280Configuration
    {
        [Required(AllowEmptyStrings = false)]
        public string? BusAddress { get; set; }
        [Required]
        public byte? DeviceAddress { get; set; }

        byte IBme280Configuration.DeviceAddress => DeviceAddress
            ?? throw new InvalidOperationException($"{nameof(DeviceAddress)} is unexpectedly null.");

        string IBme280Configuration.I2cBusAddress => BusAddress
            ?? throw new InvalidOperationException($"{nameof(BusAddress)} is unexpectedly null.");
    }
}

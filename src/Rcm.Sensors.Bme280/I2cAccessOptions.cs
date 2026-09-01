using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Rcm.Sensors.Bme280;

public class I2cAccessOptions
{
    [Required(AllowEmptyStrings = false)]
    public required string BusAddress { get; set; }

    [Required]
    [NotNull] // This has to be nullable for DataAnnotations validation to work, marking [NotNull] to avoid compiler warnings since this won't be null.
    public required byte? DeviceAddress { get; set; }
}

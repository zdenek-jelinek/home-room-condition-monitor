using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Rcm.Web.Configuration.Measurements
{
    public class MeasurementAccessModeReader
    {
        private static class MeasurementAccessModeNames
        {
            public const string I2c = "I2C";
            public const string Stub = "STUB";

            public static IEnumerable<string> All
            {
                get
                {
                    yield return I2c;
                    yield return Stub;
                }
            }
        }

        public MeasurementAccessMode Get(IConfiguration measurementAccessConfiguration)
        {
            var modeSection = measurementAccessConfiguration.GetSection("mode");
            var mode = modeSection.Value;
            if (String.IsNullOrEmpty(mode))
            {
                throw new InvalidOperationException(
                    $"Configuration variable \"{modeSection.Path}\" is not defined or empty. "
                    + $"Expected one of {GetAllApplicationModes()}.");
            }
            else if (mode.Equals(MeasurementAccessModeNames.I2c, StringComparison.InvariantCultureIgnoreCase))
            {
                return MeasurementAccessMode.I2c;
            }
            else if (mode.Equals(MeasurementAccessModeNames.Stub, StringComparison.InvariantCultureIgnoreCase))
            {
                return MeasurementAccessMode.Stub;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Configuration variable \"{modeSection.Path}\" has unrecognized value \"{mode}\". "
                    + $"Expected one of {GetAllApplicationModes()}.");
            }
        }

        private static string GetAllApplicationModes()
        {
            return String.Join(", ", MeasurementAccessModeNames.All.Select(v => $"\"{v}\""));
        }
    }
}

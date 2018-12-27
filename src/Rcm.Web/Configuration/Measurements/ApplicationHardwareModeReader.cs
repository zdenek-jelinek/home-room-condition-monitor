using System;
using System.Collections.Generic;
using System.Linq;

namespace Rcm.Web.Configuration.Measurements
{
    public class ApplicationHardwareModeReader
    {
        private static class ApplicationHardwareModeNames
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

        public ApplicationHardwareMode Get()
        {
            const string modeKey = "HW_MODE";
            var mode = Environment.GetEnvironmentVariable(modeKey);
            
            if (String.IsNullOrEmpty(mode))
            {
                throw new InvalidOperationException($"Environment varialbe \"{modeKey}\" is not defined or empty. "
                    + $"Expected one of {GetAllApplicationModes()}.");
            }

            if (mode.Equals(ApplicationHardwareModeNames.I2c))
            {
                return ApplicationHardwareMode.I2c;
            }
            else if (mode.Equals(ApplicationHardwareModeNames.Stub))
            {
                return ApplicationHardwareMode.Stub;
            }
            else
            {
                throw new InvalidOperationException($"Environment variable \"{modeKey}\" has unrecognized value \"{mode}\". "
                    + $"Expected one of {GetAllApplicationModes()}.");
            }
        }

        private static string GetAllApplicationModes()
        {
            return String.Join(", ", ApplicationHardwareModeNames.All.Select(v => $"\"{v}\""));
        }
    }
}

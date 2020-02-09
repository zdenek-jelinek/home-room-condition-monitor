using System;
using System.Threading;
using System.Threading.Tasks;
using Rcm.Common;
using Rcm.Measurement.Api;

namespace Rcm.Measurement.Stubs
{
    public class StubMeasurementProvider : IMeasurementProvider
    {
        private readonly IClock _clock;

        private readonly Random _random = new Random();

        public StubMeasurementProvider(IClock clock)
        {
            _clock = clock;
        }

        public Task<MeasurementEntry> MeasureAsync(CancellationToken token)
        {
            var now = _clock.Now;

            var baseTemperature = 15 + 10 * Math.Sin(Math.PI * now.Month / 12.0);
            var temperature = baseTemperature - 8 * Math.Sin(Math.PI * (now.Hour + 6) / 12.0);

            return Task.FromResult(
                new MeasurementEntry(
                    time: now,
                    celsiusTemperature: (decimal)temperature,
                    relativeHumidity: _random.Next(3000, 6000) / 100m,
                    hpaPressure: _random.Next(95000, 105000) / 100m));
        }
    }
}
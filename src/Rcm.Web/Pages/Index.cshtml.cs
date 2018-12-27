using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rcm.Measurement.Api;

namespace Rcm.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IMeasurementProvider _measurementProvider;

        public DateTimeOffset MeasurementTime { get; private set; }
        public decimal Temperature { get; private set; }
        public decimal Pressure { get; private set; }
        public decimal Humidity { get; private set; }

        public IndexModel(IMeasurementProvider measurementProvider)
        {
            _measurementProvider = measurementProvider;
        }

        public async Task OnGetAsync()
        {
            var measurement = await _measurementProvider.MeasureAsync();

            MeasurementTime = measurement.Time;
            Temperature = measurement.CelsiusTemperature;
            Pressure = measurement.HpaPressure;
            Humidity = measurement.RelativeHumidity;
        }
    }
}

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rcm.DataCollection.Api;

namespace Rcm.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ICollectedDataAccessor _collectedDataAccessor;

        public DateTimeOffset MeasurementTime { get; private set; }
        public decimal Temperature { get; private set; }
        public decimal Pressure { get; private set; }
        public decimal Humidity { get; private set; }
                
        public IndexModel(ICollectedDataAccessor collectedDataAccessor)
        {

            _collectedDataAccessor = collectedDataAccessor;
        }
        
        public void OnGet()
        {
            var latestMeasurement = _collectedDataAccessor
                .GetCollectedDataAsync(DateTimeOffset.MinValue, DateTimeOffset.MaxValue)
                .LastOrDefault();

            if (latestMeasurement is null)
            {
                return;
            }

            MeasurementTime = latestMeasurement.Time;
            Temperature = latestMeasurement.CelsiusTemperature;
            Pressure = latestMeasurement.HpaPressure;
            Humidity = latestMeasurement.RelativeHumidity;
        }
    }
}

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rcm.Common;
using Rcm.DataCollection.Api;

namespace Rcm.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ICollectedDataAccessor _collectedDataAccessor;

        public MeasurementEntry LatestMeasurement { get; private set; }
                
        public IndexModel(ICollectedDataAccessor collectedDataAccessor)
        {

            _collectedDataAccessor = collectedDataAccessor;
        }
        
        public void OnGet()
        {
            LatestMeasurement = _collectedDataAccessor
                .GetCollectedDataAsync(DateTimeOffset.MinValue, DateTimeOffset.MaxValue)
                .OrderBy(m => m.Time)
                .LastOrDefault();
        }
    }
}

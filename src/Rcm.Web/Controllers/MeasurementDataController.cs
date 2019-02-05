using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Rcm.DataCollection.Api;

namespace Rcm.Web.Controllers
{
    [Route("api/measurements")]
    public class MeasurementDataController : Controller
    {
        private readonly ICollectedDataAccessor _collectedDataAccessor;

        public MeasurementDataController(ICollectedDataAccessor collectedDataAccessor)
        {
            _collectedDataAccessor = collectedDataAccessor;
        }

        [HttpGet]
        public ActionResult<IEnumerable<MeasurementContract>> Get(
            [FromQuery(Name = "start")] DateTimeOffset? startTime,
            [FromQuery(Name = "end")] DateTimeOffset? endTime)
        {
            if (!startTime.HasValue || !endTime.HasValue)
            {
                BadRequest("start and end are required");
            }

            var measurements = _collectedDataAccessor.GetCollectedData(startTime.Value, endTime.Value);

            var result = measurements.Select(m => new MeasurementContract(m.Time, m.CelsiusTemperature, m.HpaPressure, m.RelativeHumidity));

            return Ok(result);
        }
    }
}

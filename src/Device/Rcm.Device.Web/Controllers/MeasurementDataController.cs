using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Rcm.Common;
using Rcm.Device.DataCollection.Api;

namespace Rcm.Device.Web.Controllers
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
            [FromQuery(Name = "end")] DateTimeOffset? endTime,
            CancellationToken token)
        {
            if (!startTime.HasValue || !endTime.HasValue)
            {
                return BadRequest("start and end are required");
            }

            var measurements = _collectedDataAccessor.GetCollectedData(startTime.Value, endTime.Value, token);

            return Ok(measurements.Select(ToContract));
        }

        private static MeasurementContract ToContract(MeasurementEntry entry)
        {
            return new MeasurementContract(
                entry.Time,
                entry.CelsiusTemperature,
                entry.HpaPressure,
                entry.RelativeHumidity);
        }
    }
}

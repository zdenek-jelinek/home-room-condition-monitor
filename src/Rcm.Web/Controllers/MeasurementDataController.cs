using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Rcm.Common;
using Rcm.Services.Measurements.Retrieval;

namespace Rcm.Web.Controllers;

[Route("api/measurements")]
public class MeasurementDataController(IMeasurementsAccessor measurementsAccessor) : Controller
{
    [HttpGet]
    public ActionResult<IEnumerable<MeasurementApiResponse>> Get(
        [FromQuery(Name = "start")] DateTimeOffset? startTime,
        [FromQuery(Name = "end")] DateTimeOffset? endTime,
        CancellationToken cancellationToken)
    {
        if (!startTime.HasValue || !endTime.HasValue)
        {
            return BadRequest("start and end are required");
        }

        var measurements = measurementsAccessor.GetMeasurements(startTime.Value, endTime.Value, cancellationToken);

        return Ok(measurements.Select(ToContract));
    }

    private static MeasurementApiResponse ToContract(MeasurementEntry entry)
    {
        return new MeasurementApiResponse(
            entry.Time,
            entry.CelsiusTemperature,
            entry.HpaPressure,
            entry.RelativeHumidity);
    }
}

using System;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Rcm.Common;
using Rcm.Services.Measurements.Retrieval;

namespace Rcm.Web.Controllers;

[ApiController]
[Route("api/measurements")]
public class MeasurementDataController(IMeasurementsAccessor measurementsAccessor) : ControllerBase
{
    [HttpGet]
    public IActionResult Get(
        [FromQuery(Name = "start")][BindRequired] DateTimeOffset startTime,
        [FromQuery(Name = "end")][BindRequired] DateTimeOffset endTime,
        CancellationToken cancellationToken)
    {
        var measurements = measurementsAccessor.GetMeasurements(startTime, endTime, cancellationToken);

        return Ok(measurements.Select(MapToResponse).ToArray());
    }

    private static MeasurementApiResponse MapToResponse(MeasurementEntry entry)
    {
        return new()
        {
            Time = entry.Time,
            CelsiusTemperature = entry.CelsiusTemperature,
            HpaPressure = entry.HpaPressure,
            RelativeHumidity = entry.RelativeHumidity
        };
    }
}

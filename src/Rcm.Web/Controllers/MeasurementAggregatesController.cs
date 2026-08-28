using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Rcm.Services.Aggregates;

namespace Rcm.Web.Controllers;

[ApiController]
[Route("api/measurements/aggregates")]
public class MeasurementAggregatesController(IMeasurementAggregatesAccessor measurementAggregatesAccessor) : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<MeasurementAggregatesApiResponse>> Get(
        [FromQuery(Name = "start")][BindRequired] DateTimeOffset startTime,
        [FromQuery(Name = "end")][BindRequired] DateTimeOffset endTime,
        [FromQuery(Name = "count")][BindRequired] int count,
        CancellationToken cancellationToken)
    {
        if (startTime > endTime)
        {
            return BadRequest($"start time is after end time: {startTime:o} > {endTime:o}");
        }

        if (count < 0)
        {
            return BadRequest($"count must be positive integer, actual is {count}");
        }

        var query = new MeasurementAggregatesQuery { StartTime = startTime, EndTime = endTime, PartitionCount = count };

        var aggregatedMeasurements = measurementAggregatesAccessor.GetMeasurementAggregates(query, cancellationToken);

        return Ok(aggregatedMeasurements.Select(MapToResponse).ToArray());
    }

    private static MeasurementAggregatesApiResponse MapToResponse(MeasurementAggregates measurementAggregates)
    {
        return new()
        {
            Temperature = MapToResponse(measurementAggregates.Temperature),
            Pressure = MapToResponse(measurementAggregates.Pressure),
            Humidity = MapToResponse(measurementAggregates.Humidity)
        };
    }

    private static AggregatesApiResponse MapToResponse(Aggregates aggregates)
    {
        return new()
        {
            First = MapToResponse(aggregates.First),
            Min = MapToResponse(aggregates.Min),
            Max = MapToResponse(aggregates.Max),
            Last = MapToResponse(aggregates.Last)
        };
    }

    private static AggregateEntryApiResponse MapToResponse(AggregateEntry entry)
    {
        return new() { Time = entry.Time, Value = entry.Value };
    }
}

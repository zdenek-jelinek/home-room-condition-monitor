using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Rcm.Services.Aggregates;

namespace Rcm.Web.Controllers;

[Route("api/measurements/aggregates")]
public class MeasurementAggregatesController : Controller
{
    private readonly IMeasurementAggregatesAccessor _measurementAggregatesAccessor;

    public MeasurementAggregatesController(IMeasurementAggregatesAccessor measurementAggregatesAccessor)
    {
        _measurementAggregatesAccessor = measurementAggregatesAccessor;
    }

    [HttpGet]
    public ActionResult<IEnumerable<MeasurementAggregatesApiResponse>> Get(
        [FromQuery(Name = "start")] DateTimeOffset? startTime,
        [FromQuery(Name = "end")] DateTimeOffset? endTime,
        [FromQuery(Name = "count")] int? count,
        CancellationToken cancellationToken)
    {
        if (!startTime.HasValue || !endTime.HasValue || !count.HasValue)
        {
            return BadRequest("start, end and count are required");
        }

        if (startTime > endTime)
        {
            return BadRequest($"start time is after end time: {startTime:o} > {endTime:o}");
        }

        if (count < 0)
        {
            return BadRequest($"count must be positive integer, actual is {count}");
        }

        var query = new MeasurementAggregatesQuery { StartTime = startTime.Value, EndTime = endTime.Value, PartitionCount = count.Value };

        var aggregatedMeasurements = _measurementAggregatesAccessor.GetMeasurementAggregates(query, cancellationToken);

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

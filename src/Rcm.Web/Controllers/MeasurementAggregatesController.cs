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
    public IActionResult Get(
        [FromQuery(Name = "start")][BindRequired] DateTimeOffset startTime,
        [FromQuery(Name = "end")][BindRequired] DateTimeOffset endTime,
        [FromQuery(Name = "count")][BindRequired] int count,
        CancellationToken cancellationToken)
    {
        if (startTime > endTime)
        {
            return ValidationProblem(Error(property: "startTime", message: "start time must be earlier than end time"));
        }

        if (count <= 0)
        {
            return ValidationProblem(Error(property: "count", message: "count must be a positive integer"));
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

    private static AggregatesApiResponse MapToResponse(MeasurementDimensionAggregates aggregates)
    {
        return new()
        {
            First = MapToResponse(aggregates.First),
            Min = MapToResponse(aggregates.Min),
            Max = MapToResponse(aggregates.Max),
            Last = MapToResponse(aggregates.Last)
        };
    }

    private static AggregateEntryApiResponse MapToResponse(MeasurementAggregatesEntry entry)
    {
        return new() { Time = entry.Time, Value = entry.Value };
    }

    private static ValidationProblemDetails Error(string property, string message)
    {
        return new(new Dictionary<string, string[]> { [property] = [message] });
    }
}

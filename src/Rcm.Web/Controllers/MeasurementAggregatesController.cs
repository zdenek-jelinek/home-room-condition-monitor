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

    private static MeasurementAggregatesApiResponse MapToResponse(MeasurementAggregates aggregates)
    {
        return new()
        {
            Temperature = MapToResponse(aggregates.Temperature),
            Pressure = MapToResponse(aggregates.Pressure),
            Humidity = MapToResponse(aggregates.Humidity)
        };
    }

    private static MeasurementAggregatesDimensionApiResponse MapToResponse(MeasurementDimensionAggregates dimension)
    {
        return new()
        {
            First = MapToResponse(dimension.First),
            Min = MapToResponse(dimension.Min),
            Max = MapToResponse(dimension.Max),
            Last = MapToResponse(dimension.Last)
        };
    }

    private static MeasurementAggregatesEntryApiResponse MapToResponse(MeasurementAggregatesEntry entry)
    {
        return new() { Time = entry.Time, Value = entry.Value };
    }

    private static ValidationProblemDetails Error(string property, string message)
    {
        return new(new Dictionary<string, string[]> { [property] = [message] });
    }
}

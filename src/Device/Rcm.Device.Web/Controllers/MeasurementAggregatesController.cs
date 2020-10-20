using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Rcm.Device.Aggregates.Api;

namespace Rcm.Device.Web.Controllers
{
    [Route("api/measurements/aggregates")]
    public class MeasurementAggregatesController : Controller
    {
        private readonly IMeasurementAggregatesAccessor _measurementAggregatesAccessor;

        public MeasurementAggregatesController(IMeasurementAggregatesAccessor measurementAggregatesAccessor)
        {
            _measurementAggregatesAccessor = measurementAggregatesAccessor;
        }

        [HttpGet]
        public ActionResult<IEnumerable<MeasurementAggregatesContract>> Get(
            [FromQuery(Name = "start")] DateTimeOffset? startTime,
            [FromQuery(Name = "end")] DateTimeOffset? endTime,
            [FromQuery(Name = "count")] int? count,
            CancellationToken token)
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

            var aggregatedMeasurements = _measurementAggregatesAccessor
                .GetMeasurementAggregates(startTime.Value, endTime.Value, count.Value, token);

            var result = aggregatedMeasurements
                .Select(m => new MeasurementAggregatesContract(
                    temperature: MapAggregates(m.Temperature),
                    pressure: MapAggregates(m.Pressure),
                    humidity: MapAggregates(m.Humidity)));

            return Ok(result);
        }

        private static AggregatesContract MapAggregates(Aggregates.Api.Aggregates aggregates)
        {
            return new AggregatesContract(
                MapAggregateEntry(aggregates.First),
                MapAggregateEntry(aggregates.Min),
                MapAggregateEntry(aggregates.Max),
                MapAggregateEntry(aggregates.Last));
        }

        private static AggregateEntryContract MapAggregateEntry(AggregateEntry entry)
        {
            return new AggregateEntryContract(entry.Time, entry.Value);
        }
    }
}

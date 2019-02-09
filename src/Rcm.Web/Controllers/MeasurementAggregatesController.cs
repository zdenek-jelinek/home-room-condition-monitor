﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Rcm.Aggregates.Api;

namespace Rcm.Web.Controllers
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
        public ActionResult<IEnumerable<MeasurementContract>> Get(
            [FromQuery(Name = "start")] DateTimeOffset? startTime,
            [FromQuery(Name = "end")] DateTimeOffset? endTime,
            [FromQuery(Name = "count")] int? count)
        {
            if (!startTime.HasValue || !endTime.HasValue || !count.HasValue)
            {
                BadRequest("start, end and count are required");
            }

            if (startTime > endTime)
            {
                BadRequest($"start time is after end time: {startTime:o} > {endTime:o}");
            }

            if (count < 0)
            {
                BadRequest($"count must be positive integer, actual is {count}");
            }

            var aggregatedMeasurements = _measurementAggregatesAccessor.GetMeasurementAggregates(startTime.Value, endTime.Value, count.Value);

            var result = aggregatedMeasurements.Select(m => new MeasurementAggregatesContract(
                MapAggregates(m.Temperature), 
                MapAggregates(m.Pressure), 
                MapAggregates(m.Humidity)));

            return Ok(result);
        }

        private AggregatesContract MapAggregates(Aggregates.Api.Aggregates aggregates)
        {
            return new AggregatesContract(
                MapAggregateEntry(aggregates.First),
                MapAggregateEntry(aggregates.Min),
                MapAggregateEntry(aggregates.Max),
                MapAggregateEntry(aggregates.Last));
        }

        private AggregateEntryContract MapAggregateEntry(AggregateEntry entry)
        {
            return new AggregateEntryContract(entry.Time, entry.Value);
        }
    }
}
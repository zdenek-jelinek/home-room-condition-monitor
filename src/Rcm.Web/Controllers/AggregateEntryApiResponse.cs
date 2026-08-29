using System;

namespace Rcm.Web.Controllers;

public sealed class MeasurementAggregatesEntryApiResponse
{
    public required DateTimeOffset Time { get; init; }
    public required decimal Value { get; init; }
}

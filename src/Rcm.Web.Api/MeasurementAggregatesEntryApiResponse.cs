using System;

namespace Rcm.Web.Api;

public sealed class MeasurementAggregatesEntryApiResponse
{
    public required DateTimeOffset Time { get; init; }
    public required decimal Value { get; init; }
}

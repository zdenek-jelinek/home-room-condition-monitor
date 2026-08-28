using System;

namespace Rcm.Web.Controllers;

public sealed class AggregateEntryApiResponse
{
    public required DateTimeOffset Time { get; init; }
    public required decimal Value { get; init; }
}

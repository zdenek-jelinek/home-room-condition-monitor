using System;

namespace Rcm.Web.Controllers;

public class AggregateEntryApiResponse
{
    public DateTimeOffset Time { get; }
    public decimal Value { get; }

    public AggregateEntryApiResponse(DateTimeOffset time, decimal value)
    {
        Time = time;
        Value = value;
    }
}

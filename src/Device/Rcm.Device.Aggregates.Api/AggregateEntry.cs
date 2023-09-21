using System;

namespace Rcm.Device.Aggregates.Api;

public class AggregateEntry
{
    public DateTimeOffset Time { get; }
    public decimal Value { get; }

    public AggregateEntry(DateTimeOffset time, decimal value)
    {
        Time = time;
        Value = value;
    }
}
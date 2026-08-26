using System;

namespace Rcm.Services.Aggregates;

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

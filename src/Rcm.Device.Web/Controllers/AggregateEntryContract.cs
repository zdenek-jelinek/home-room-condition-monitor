using System;

namespace Rcm.Device.Web.Controllers;

public class AggregateEntryContract
{
    public DateTimeOffset Time { get; }
    public decimal Value { get; }

    public AggregateEntryContract(DateTimeOffset time, decimal value)
    {
        Time = time;
        Value = value;
    }
}
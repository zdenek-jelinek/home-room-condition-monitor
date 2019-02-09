using System;

namespace Rcm.Web.Controllers
{
    internal class AggregateEntryContract
    {
        public DateTimeOffset Time { get; }
        public decimal Value { get; }

        public AggregateEntryContract(DateTimeOffset time, decimal value)
        {
            Time = time;
            Value = value;
        }
    }
}
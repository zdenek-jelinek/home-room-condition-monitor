namespace Rcm.Web.Controllers
{
    internal class AggregatesContract
    {
        public AggregateEntryContract First { get; }
        public AggregateEntryContract Min { get; }
        public AggregateEntryContract Max { get; }
        public AggregateEntryContract Last { get; }

        public AggregatesContract(AggregateEntryContract first, AggregateEntryContract min, AggregateEntryContract max, AggregateEntryContract last)
        {
            First = first;
            Min = min;
            Max = max;
            Last = last;
        }
    }
}
namespace Rcm.Aggregates.Api
{
    public class Aggregates
    {
        public AggregateEntry First { get; }
        public AggregateEntry Min { get; }
        public AggregateEntry Max { get; }
        public AggregateEntry Last { get; }

        public Aggregates(AggregateEntry first, AggregateEntry min, AggregateEntry max, AggregateEntry last)
        {
            First = first;
            Min = min;
            Max = max;
            Last = last;
        }
    }
}
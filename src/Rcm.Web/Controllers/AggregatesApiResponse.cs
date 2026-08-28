namespace Rcm.Web.Controllers;

public class AggregatesApiResponse
{
    public AggregateEntryApiResponse First { get; }
    public AggregateEntryApiResponse Min { get; }
    public AggregateEntryApiResponse Max { get; }
    public AggregateEntryApiResponse Last { get; }

    public AggregatesApiResponse(AggregateEntryApiResponse first, AggregateEntryApiResponse min, AggregateEntryApiResponse max, AggregateEntryApiResponse last)
    {
        First = first;
        Min = min;
        Max = max;
        Last = last;
    }
}

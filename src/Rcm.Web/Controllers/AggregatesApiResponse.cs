namespace Rcm.Web.Controllers;

public sealed class AggregatesApiResponse
{
    public required AggregateEntryApiResponse First { get; init; }
    public required AggregateEntryApiResponse Min { get; init; }
    public required AggregateEntryApiResponse Max { get; init; }
    public required AggregateEntryApiResponse Last { get; init; }
}

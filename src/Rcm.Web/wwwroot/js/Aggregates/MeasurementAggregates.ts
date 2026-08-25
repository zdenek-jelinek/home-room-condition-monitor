export interface MeasurementAggregates
{
    readonly temperature:Aggregates;
    readonly pressure:Aggregates;
    readonly humidity:Aggregates;
}

export interface Aggregates
{
    readonly first:AggregateEntry;
    readonly min:AggregateEntry;
    readonly max:AggregateEntry;
    readonly last:AggregateEntry;
}

export interface AggregateEntry
{
    readonly time:Date;
    readonly value:number;
}
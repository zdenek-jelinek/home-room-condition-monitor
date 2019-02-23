import { MeasurementAggregates, Aggregates, AggregateEntry } from "./MeasurementAggregates";

export class MeasurementAggregatesClient
{
    public async getMeasurementsAsync(start:Date, end:Date, count:number):Promise<ReadonlyArray<MeasurementAggregates>>
    {
        const response = await fetch(`/api/measurements/aggregates?start=${start.toISOString()}&end=${end.toISOString()}&count=${count}`);

        const aggregates:ReadonlyArray<MeasurementAggregatesContract> = await response.json();
        
        return aggregates.map(e => ({
            temperature: this.mapToAggregates(e.temperature),
            pressure: this.mapToAggregates(e.pressure),
            humidity: this.mapToAggregates(e.humidity)
        }));
    }

    private mapToAggregates(contract:AggregatesContract):Aggregates
    {
        return {
            first: this.mapToAggregateEntry(contract.first),
            min: this.mapToAggregateEntry(contract.min),
            max: this.mapToAggregateEntry(contract.max),
            last: this.mapToAggregateEntry(contract.last)
        };
    }

    private mapToAggregateEntry(contract:AggregateEntryContract):AggregateEntry
    {
        return {
            time: new Date(contract.time),
            value: contract.value
        };
    }
}

interface MeasurementAggregatesContract
{
    temperature:AggregatesContract;
    pressure:AggregatesContract;
    humidity:AggregatesContract;
}

interface AggregatesContract
{
    first:AggregateEntryContract;
    min:AggregateEntryContract;
    max:AggregateEntryContract;
    last:AggregateEntryContract;
}

interface AggregateEntryContract
{
    time:string;
    value:number;
}
import { Measurement } from "./Measurement";

export class MeasurementsClient
{
    public async getMeasurementsAsync(start:Date, end:Date):Promise<Measurement[]>
    {
        const response = await fetch(`/api/measurements?start=${start.toISOString()}&end=${end.toISOString()}`);

        const measurements:ReadonlyArray<MeasurementContract> = await response.json();
        
        return measurements.map(e => new Measurement(new Date(e.time), e.celsiusTemperature, e.hpaPressure, e.relativeHumidity));
    }

    public async getLatestMeasurementAsync():Promise<Measurement|undefined>
    {
        const now = new Date();
        const fiveMinutesInMilliseconds = 5 * 60 * 1000;
        const fiveMinutesAgo = new Date(now.getTime() - fiveMinutesInMilliseconds);

        const measurements:Measurement[] = await this.getMeasurementsAsync(fiveMinutesAgo, now);

        // sort by time descending
        measurements.sort((a, b) => b.time.getTime() - a.time.getTime());

        return measurements[0];
    }
}

interface MeasurementContract
{
    time:string;
    celsiusTemperature:number;
    hpaPressure:number;
    relativeHumidity:number;
}
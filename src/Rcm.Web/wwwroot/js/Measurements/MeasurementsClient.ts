import { Measurement } from "./Measurement";

export class MeasurementsClient
{
    async getMeasurementsAsync(start:Date, end:Date):Promise<ReadonlyArray<Measurement>>
    {
        const response = await fetch(`/api/measurements?start=${start.toISOString()}&end=${end.toISOString()}`);

        const measurements:ReadonlyArray<MeasurementContract> = await response.json();
        
        return measurements.map(e => new Measurement(new Date(e.time), e.celsiusTemperature, e.hpaPressure, e.relativeHumidity));
    }
}

interface MeasurementContract
{
    time:string;
    celsiusTemperature:number;
    hpaPressure:number;
    relativeHumidity:number;
}
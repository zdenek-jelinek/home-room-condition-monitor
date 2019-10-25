import { DataLifecyclePage } from "./Common/DataLifecyclePage";
import { MeasurementsClient } from "../Measurements/MeasurementsClient";
import { Measurement } from "../Measurements/Measurement";


export function initialize()
{
    const measurementsClient = new MeasurementsClient();
    instance = new NowPage(measurementsClient);
}

let instance:NowPage;

export class NowPage extends DataLifecyclePage
{
    private readonly _measurementsClient:MeasurementsClient;

    constructor(measurementsClient:MeasurementsClient)
    {
        super("loading");
        this._measurementsClient = measurementsClient;

        setInterval(this.populateData.bind(this), 10000);

        this.populateData();
    }

    private async populateData(this:NowPage):Promise<void>
    {
        const latestMeasurement = await this._measurementsClient.getLatestMeasurementAsync();
        if (!latestMeasurement)
        {
            this.dataAvailable(false);
            return;
        }

        this.populate(latestMeasurement);

        this.dataAvailable(true);
    }

    private populate(measurement:Measurement):void
    {
        this.setText("#temperature-value", measurement.temperature);
        this.setText("#pressure-value", measurement.pressure);
        this.setText("#humidity-value", measurement.humidity);
    }

    private setText(selector:string, value:number):void
    {
        this.setElementText(document, selector, value.toFixed(1));
    }
}
import { MeasurementAggregatesClient } from "../Aggregates/MeasurementAggregatesClient";
import { DataLifecyclePage } from "./Common/DataLifecyclePage";
import { MeasurementAggregates, Aggregates } from "../Aggregates/MeasurementAggregates";

export function initialize()
{
    const aggregatesClient = new MeasurementAggregatesClient();
    instance = new DailyPage(aggregatesClient);
}

let instance:DailyPage;

export class DailyPage extends DataLifecyclePage
{
    private readonly _aggregatesClient:MeasurementAggregatesClient;

    constructor(aggregatesClient:MeasurementAggregatesClient)
    {
        super("loading");
        this._aggregatesClient = aggregatesClient;

        setInterval(this.populateData.bind(this), 60000);

        this.populateData();
    }

    private async populateData(this:DailyPage):Promise<void>
    {
        const data = await this.fetchData();
        if (!data)
        {
            this.dataAvailable(false);
            return;
        }

        this.populateUi(data.temperature);

        this.dataAvailable(true);
    }

    private populateUi(aggregates:Aggregates):void
    {
        const components:ReadonlyArray<{ node:string, field:keyof(Aggregates) }> = [
            { node: "#latest", field: "last" },
            { node: "#min", field: "min" },
            { node: "#max", field: "max" },
            { node: "#first", field: "first" }
        ];

        for (const component of components)
        {
            const node = document.querySelector(component.node);
            if (!node || !(node instanceof HTMLElement))
            {
                throw new Error(`Could not find element ${component.node} to assign data`);
            }

            const aggregate = aggregates[component.field];
            const value = (Math.round(aggregate.value * 10) / 10).toFixed(1);
            this.setElementText(node, ".time.value", aggregate.time.toLocaleTimeString());
            this.setElementText(node, ".temperature.value", value.toString());
        }
    }

    private setElementText(parent:Element, selector:string, text:string):void
    {
        const element = parent.querySelector(selector);
        if (!element)
        {
            throw new Error("Could not set value: No element with selector " + selector + " found");
        }

        element.textContent = text;
    }

    private async fetchData():Promise<MeasurementAggregates|undefined>
    {
        const startOfToday = new Date();
        startOfToday.setHours(0, 0, 0, 0);
        const endOfToday = new Date(startOfToday);
        endOfToday.setHours(23, 59, 59, 999);

        const aggregates = await this._aggregatesClient.getMeasurementsAsync(startOfToday, endOfToday, 1);

        if (aggregates.length === 1)
        {
            return aggregates[0];
        }

        return undefined;
    }
}
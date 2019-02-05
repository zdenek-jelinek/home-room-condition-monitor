import { DateRange, DateRangePicker } from "./DateRangePicker";
import { MeasurementsClient } from "./MeasurementsClient";
import { Measurement } from "./Measurement";

export function initialize():void
{
    const startDateInput = <HTMLInputElement>getElement("input#time-range");

    const weekInMilliseconds = 7 * 86400 * 1000;
    const now = new Date();
    const oneWeekAgo = new Date(now.getTime() - weekInMilliseconds);

    const dateRangePicker = new DateRangePicker(startDateInput, { start: oneWeekAgo, end: now });

    const measurementsClient = new MeasurementsClient();
    
    instance = new IndexPage(
        getElement("#measurements-loading"),
        getElement("#measurements-none"),
        getElement("#measurements-available"),
        dateRangePicker,
        measurementsClient);
}

function getElement(selector:string):HTMLElement
{
    const element = document.querySelector(selector);
    if (!element || !(element instanceof HTMLElement))
    {
        throw new Error("Could not access element " + selector);
    }

    return element;
}

let instance:IndexPage;

class IndexPage
{
    private readonly measurementsLoading:HTMLElement;
    private readonly measurementsNone:HTMLElement;
    private readonly measurementsAvailable:HTMLElement;
    private readonly dateRangePicker:DateRangePicker;
    private readonly measurementsClient:MeasurementsClient;

    constructor(
        measurementsLoading: HTMLElement,
        measurementsNone: HTMLElement,
        measurementsAvailable: HTMLElement,
        dateRangePicker:DateRangePicker,
        measurementsClient:MeasurementsClient)
    {
        this.measurementsLoading = measurementsLoading;
        this.measurementsNone = measurementsNone;
        this.measurementsAvailable = measurementsAvailable;
        this.dateRangePicker = dateRangePicker;
        this.measurementsClient = measurementsClient;

        dateRangePicker.addListener(this.dateTimeRangeChanged.bind(this));

        this.loadData(dateRangePicker.selectedRange);
    }

    private dateTimeRangeChanged(this:IndexPage, dates?:DateRange):void
    {
        this.loadData(dates);
    }

    private async loadData(dates?:DateRange):Promise<void>
    {
        if (!dates)
        {
            this.showNoData();
            return;
        }

        this.showLoading();

        const measurements = await this.measurementsClient.getMeasurementsAsync(dates.start, dates.end);
        if (measurements.length === 0)
        {
            this.showNoData();
        }
        else
        {
            this.showAvailableData(measurements);
        }

    }

    private showLoading(this:IndexPage)
    {
        this.measurementsLoading.style.display = "flex";
        this.measurementsAvailable.style.display = "none";
        this.measurementsNone.style.display = "none";
    }

    private showNoData(this:IndexPage)
    {
        this.measurementsNone.style.display = "block";
        this.measurementsLoading.style.display = "none";
        this.measurementsAvailable.style.display = "none";
    }

    private showAvailableData(this:IndexPage, measurements:ReadonlyArray<Measurement>)
    {
        this.measurementsAvailable.style.display = "block";
        this.measurementsLoading.style.display = "none";
        this.measurementsNone.style.display = "none";

        const measurementsTable = <HTMLTableElement>getElement("#measurements-available table");
        for (const body of measurementsTable.tBodies)
        {
            body.remove();
        }

        const body = measurementsTable.createTBody();
        
        for (const measurement of measurements)
        {
            const row = body.insertRow();
            
            this.createCell(row, measurement.time.toLocaleString());
            this.createCell(row, measurement.temperature.toLocaleString());
            this.createCell(row, measurement.pressure.toLocaleString());
            this.createCell(row, measurement.humidity.toLocaleString());
        }
    }

    private createCell(row:HTMLTableRowElement, value:string):void
    {
        const text = document.createTextNode(value);
        row.insertCell().appendChild(text);
    }
}
import { DateRange, DateRangePicker } from "./DateRangePicker";

export function initialize():void
{
    const startDateInput = <HTMLInputElement>getElement("input#time-range");

    const weekInMilliseconds = 7 * 86400 * 1000;
    const now = new Date();
    const oneWeekAgo = new Date(now.getTime() - weekInMilliseconds);

    const dateRangePicker = new DateRangePicker(startDateInput, { start: oneWeekAgo, end: now });
    
    instance = new IndexPage(
        getElement("#measurements-loading"),
        getElement("#measurements-none"),
        getElement("#measurements-available"),
        dateRangePicker);
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
    //private readonly measurementDataClient:MeasurementDataClient;

    constructor(
        measurementsLoading: HTMLElement,
        measurementsNone: HTMLElement,
        measurementsAvailable: HTMLElement,
        dateRangePicker:DateRangePicker)
    {
        this.measurementsLoading = measurementsLoading;
        this.measurementsNone = measurementsNone;
        this.measurementsAvailable = measurementsAvailable;
        this.dateRangePicker = dateRangePicker;

        dateRangePicker.addListener(this.dateTimeRangeChanged.bind(this));

        this.showNoData();
    }

    private dateTimeRangeChanged(this:IndexPage, dates?:DateRange):void
    {
        if (!dates)
        {
            this.showNoData();
            return;
        }

        this.showLoading();
        // query new range of data
        
        setTimeout(this.showNoData.bind(this), 2000);
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

    private showAvailableData(this:IndexPage)
    {
        this.measurementsAvailable.style.display = "block";
        this.measurementsLoading.style.display = "none";
        this.measurementsNone.style.display = "none";
    }
}
import flatpickr from "flatpickr";

export interface DateRange
{
    start:Date;
    end:Date;
}

export class DateRangePicker
{
    private readonly _instance:flatpickr.Instance;
    private readonly _listeners: Array<(dates?:DateRange) => void> = [];

    public get selectedRange():{start:Date, end:Date}|undefined
    {
        const dates = this._instance.selectedDates;
        if (dates.length !== 2)
        {
            return undefined;
        }

        return { start: dates[0], end: dates[1] };
    }

    constructor(input:HTMLInputElement, initialDates?:{start:Date, end:Date})
    {
        this._instance = <flatpickr.Instance>flatpickr(
            input, 
            {
                time_24hr: true,
                closeOnSelect: true,
                mode: "range",
                onChange: this.dateRangeChanged.bind(this),
                defaultDate: initialDates && [initialDates.start, initialDates.end]
            });
    }

    private dateRangeChanged(this: DateRangePicker, dates:Date[]):void
    {
        const newRange = dates.length === 2
            ? { start: dates[0], end: dates[1] }
            : undefined;

        this._listeners.forEach(l => l(newRange));
    }

    public addListener(listener:(dates?:DateRange) => void):void
    {
        this._listeners.push(listener);
    }

    public removeListener(listener: (dates?: DateRange) => void):void
    {
        const index = this._listeners.indexOf(listener);
        if (index >= 0)
        {
            this._listeners.splice(index, 1);
        }
    }
}
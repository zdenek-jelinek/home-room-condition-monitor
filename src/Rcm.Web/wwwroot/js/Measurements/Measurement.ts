export class Measurement
{
    constructor(
        readonly time:Date,
        readonly temperature:number,
        readonly pressure:number,
        readonly humidity:number)
    {
    }
}
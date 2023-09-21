namespace Rcm.Common;

public static class DateTimeFormat
{
    private const string Date = "yyyy-MM-dd";
    private const string Time = "HH':'mm':'ss.FFFFFFF";
    private const string Zone = "K";

    public static string Iso8601DateTime => Date + "T" + Time + Zone;
    public static string Iso8601Date => Date;
    public static string Iso8601Time => Time;
}
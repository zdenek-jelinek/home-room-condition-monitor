namespace Rcm.Backend.Device.Api;

public class MeasurementEntryIngressModel
{
    /// <summary>
    /// ISO-8601 date time
    /// </summary>
    public string? Time { get; set; }

    /// <summary>
    /// Temperature [°C]
    /// </summary>
    public decimal? Temperature { get; set; }

    /// <summary>
    /// Pressure [hPa]
    /// </summary>
    public decimal? Pressure { get; set; }
        
    /// <summary>
    /// Relative humidity [%]
    /// </summary>
    public decimal? Humidity { get; set; }
}
namespace Rcm.Sensors.Bme280;

public class TemperatureCalculator(TemperatureCompensationParameters compensationParameters)
{
    // The calculation logic in the following method is adapted from BME280 data sheet, chapter 4.2.3 Compensation Formulas
    public (decimal temperature, int fineTemperature) CalculateTemperature(int rawTemperature)
    {
        var firstPart = (((rawTemperature >> 3) - (compensationParameters.Temperature1 << 1)) * compensationParameters.Temperature2) >> 11;
        var secondPart = ((rawTemperature >> 4) - compensationParameters.Temperature1);
        secondPart *= secondPart;
        secondPart = ((secondPart >> 12) * compensationParameters.Temperature3) >> 14;

        var fineTemperature = firstPart + secondPart;
        var temperature = ((fineTemperature * 5 + 128) >> 8) / 100m;
        return (temperature, fineTemperature);
    }
}

namespace Rcm.Device.Bme280
{
    public class TemperatureCalculator
    {
        private readonly TemperatureCompensationParameters _compensationParameters;

        public TemperatureCalculator(TemperatureCompensationParameters compensationParameters)
        {
            _compensationParameters = compensationParameters;
        }

        // The calculation logic in the following method is adapted from BME280 data sheet, chapter 4.2.3 Compensation Formulas
        public (decimal temperature, int fineTemperature) CalculateTemperature(int rawTemperature)
        {
            var firstPart  = (((rawTemperature >> 3) - (_compensationParameters.Temperature1 << 1)) * _compensationParameters.Temperature2) >> 11;
            var secondPart = ((rawTemperature >> 4) - _compensationParameters.Temperature1);
            secondPart *= secondPart;
            secondPart = ((secondPart >> 12) * _compensationParameters.Temperature3) >> 14;

            var fineTemperature = firstPart + secondPart;
            var temperature  = ((fineTemperature * 5 + 128) >> 8) / 100m;
            return (temperature, fineTemperature);
        }
    }
}
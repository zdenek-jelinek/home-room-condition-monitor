namespace Rcm.Device.Bme280
{
    public class TemperatureCompensationParameters
    {
        public int Temperature1 { get; }
        public int Temperature2 { get; }
        public int Temperature3 { get; }

        public TemperatureCompensationParameters(int temperature1, int temperature2, int temperature3)
        {
            Temperature1 = temperature1;
            Temperature2 = temperature2;
            Temperature3 = temperature3;
        }
    }
}
namespace Rcm.Bme280
{
    public class HumidityCompensationParameters
    {
        public int Humidity1 { get; }
        public int Humidity2 { get; }
        public int Humidity3 { get; }
        public int Humidity4 { get; }
        public int Humidity5 { get; }
        public int Humidity6 { get; }

        public HumidityCompensationParameters(
            int humidity1,
            int humidity2,
            int humidity3,
            int humidity4,
            int humidity5,
            int humidity6)
        {
            Humidity1 = humidity1;
            Humidity2 = humidity2;
            Humidity3 = humidity3;
            Humidity4 = humidity4;
            Humidity5 = humidity5;
            Humidity6 = humidity6;
        }
    }
}
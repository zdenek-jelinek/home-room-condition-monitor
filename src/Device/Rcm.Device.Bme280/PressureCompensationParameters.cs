namespace Rcm.Device.Bme280
{
    public class PressureCompensationParameters
    {
        public long Pressure1 { get; }
        public long Pressure2 { get; }
        public long Pressure3 { get; }
        public long Pressure4 { get; }
        public long Pressure5 { get; }
        public long Pressure6 { get; }
        public long Pressure7 { get; }
        public long Pressure8 { get; }
        public long Pressure9 { get; }

        public PressureCompensationParameters(
            long pressure1,
            long pressure2,
            long pressure3,
            long pressure4,
            long pressure5,
            long pressure6,
            long pressure7,
            long pressure8,
            long pressure9)
        {
            Pressure1 = pressure1;
            Pressure2 = pressure2;
            Pressure3 = pressure3;
            Pressure4 = pressure4;
            Pressure5 = pressure5;
            Pressure6 = pressure6;
            Pressure7 = pressure7;
            Pressure8 = pressure8;
            Pressure9 = pressure9;
        }
    }
}
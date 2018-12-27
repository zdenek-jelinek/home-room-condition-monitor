using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rcm.Common;
using Rcm.I2c;
using Rcm.Measurement.Api;

namespace Rcm.Bme280
{
    public class Bme280I2cDevice : IMeasurementProvider, IDisposable
    {
        private readonly byte _address;
        private readonly I2cBus _bus;
        private readonly IClock _clock;
        private readonly Lazy<CompensationParameters> _compensationParameters;
        private readonly ILogger<Bme280I2cDevice> _logger;

        public Bme280I2cDevice(ILogger<Bme280I2cDevice> logger, IClock clock, I2cBus bus, byte address)
        {
            _logger = logger;
            _bus = bus;
            _address = address;
            _clock = clock;

            _compensationParameters = new Lazy<CompensationParameters>(ReadCompensationParameters);
        }

        public async Task<MeasurementEntry> MeasureAsync()
        {
            _logger.LogDebug("Initiating measurement...");
            InitiateMeasurement();

            var measurementStart = DateTime.Now;
            do
            {
                await Task.Delay(100);
            }
            while (IsMeasurementInProgress());

            var measurementTime = DateTime.Now - measurementStart;
            if (measurementTime > TimeSpan.FromMilliseconds(100))
            {
                _logger.LogWarning($"Measurement took {measurementTime.TotalMilliseconds}ms");
            }
            else
            {
                _logger.LogDebug($"Measurement took {measurementTime.TotalMilliseconds}ms");
            }

            _logger.LogDebug("Reading measurement results...");
            var (rawPressure, rawTemperature, rawHumidity) = ReadMeasurementResults();

            _logger.LogDebug("Compensating measurement results...");
            return CompensateResults(rawPressure, rawTemperature, rawHumidity, _compensationParameters.Value);
        }

        private MeasurementEntry CompensateResults(
            int rawPressure,
            int rawTemperature,
            int rawHumidity,
            CompensationParameters compensationParameters)
        {
            var temperatureCalculator = new TemperatureCalculator(compensationParameters.Temperature);
            var humidityCalculator = new HumidityCalculator(compensationParameters.Humidity);

            var (resultingTemperature, fineTemperature) = temperatureCalculator.CalculateTemperature(rawTemperature);

            var pressure = CompensatePressure(rawPressure, fineTemperature, compensationParameters.Pressure);

            var humidity = humidityCalculator.CalculateHumidity(rawHumidity, fineTemperature);

            _logger.LogTrace($"Compensated values as {resultingTemperature}°C, {pressure}hPa, {humidity}%rH");

            return new MeasurementEntry(
                _clock.Now,
                resultingTemperature,
                humidity,
                pressure);
        }

        private decimal CompensatePressure(
            int rawPressure,
            int fineTemperature,
            PressureCompensationParameters compensation)
        {
            var v1 = fineTemperature - 128000L;
            var v1_sq = v1 * v1;
            var v2 = v1_sq * compensation.Pressure6
                + ((v1 * compensation.Pressure5) << 17)
                + (compensation.Pressure4 << 35);

            v1 = ((v1_sq * compensation.Pressure3) >> 8) + ((v1 * compensation.Pressure2) << 12);
            v1 = (((1L << 47) + v1) * compensation.Pressure1) >> 33;

            if (v1 == 0)
            {
                return 0m;
            }

            var p = 1048576L - rawPressure;
            p = ((p << 31) - v2) * 3125 / v1;
            v1 = (compensation.Pressure9 * (p >> 13) * (p >> 13)) >> 25;
            v2 = (compensation.Pressure8 * p) >> 19;
            p = ((p + v1 + v2) >> 8) + (compensation.Pressure7 << 4);

            return p / 256m / 100m;
        }

        private (int pressure, int temperature, int humidity) ReadMeasurementResults()
        {
            const byte firstMeasurementResultRegisterAddress = 0xF7;
            const byte resultRegistersSize = 0xFE - firstMeasurementResultRegisterAddress + 1;

            Span<byte> results = stackalloc byte[resultRegistersSize];
            Read(firstMeasurementResultRegisterAddress, results);

            var pressure = (results[0] << 12) | (results[1] << 4) | (results[2] >> 4);
            var temperature = (results[3] << 12) | (results[4] << 4) | (results[5] >> 4);
            var humidity = (results[6] << 8) | results[7];

            _logger.LogTrace($"Read pressure {pressure:X5}, temperature {temperature:X5}, humidity {humidity:X4}");

            return (pressure, temperature, humidity);
        }

        private void InitiateMeasurement()
        {
            const byte humiditySettingsRegisterAddress = 0xF2;
            Write(humiditySettingsRegisterAddress, (byte)Oversampling.X8);

            const byte measurementControlRegisterAddress = 0xF4;
            const byte temperatureOversampling = (byte)Oversampling.X8;
            const byte pressureOversampling = (byte)Oversampling.X16;
            const int forcedMode = 0b10;

            const byte controlValue =
                ((temperatureOversampling << 5) | (pressureOversampling << 2) | forcedMode) & 0xFF;
            Write(measurementControlRegisterAddress, controlValue);
        }

        private CompensationParameters ReadCompensationParameters()
        {
            _logger.LogDebug("Loading compensation parameters.");

            const int lowCompensationRegistersStartAddress = 0x88;
            const int lowCompensationRegistersSize = 0xA1 - lowCompensationRegistersStartAddress + 1;
            Span<byte> lowCompensation = stackalloc byte[lowCompensationRegistersSize];

            Read(lowCompensationRegistersStartAddress, lowCompensation);

            const int highCompensationRegistersStartAddress = 0xE1;
            const int highCompensationRegistersSize = 0xE7 - highCompensationRegistersStartAddress + 1;
            Span<byte> highCompensation = stackalloc byte[highCompensationRegistersSize];

            Read(highCompensationRegistersStartAddress, highCompensation);

            unchecked
            {
                var temperatureCompensation = new TemperatureCompensationParameters(
                    temperature1: (ushort)(lowCompensation[0] | (lowCompensation[1] << 8)),
                    temperature2: (short)(lowCompensation[2] | (lowCompensation[3] << 8)),
                    temperature3: (short)(lowCompensation[4] | (lowCompensation[5] << 8)));


                var pressureCompensation = new PressureCompensationParameters(
                    pressure1: (ushort)(lowCompensation[6] | (lowCompensation[7] << 8)),
                    pressure2: (short)(lowCompensation[8] | (lowCompensation[9] << 8)),
                    pressure3: (short)(lowCompensation[10] | (lowCompensation[11] << 8)),
                    pressure4: (short)(lowCompensation[12] | (lowCompensation[13] << 8)),
                    pressure5: (short)(lowCompensation[14] | (lowCompensation[15] << 8)),
                    pressure6: (short)(lowCompensation[16] | (lowCompensation[17] << 8)),
                    pressure7: (short)(lowCompensation[18] | (lowCompensation[19] << 8)),
                    pressure8: (short)(lowCompensation[20] | (lowCompensation[21] << 8)),
                    pressure9: (short)(lowCompensation[22] | (lowCompensation[23] << 8)));

                var humidityCompensation = new HumidityCompensationParameters(
                    humidity1: lowCompensation[25],
                    humidity2: (short)(highCompensation[0] | (highCompensation[1] << 8)),
                    humidity3: highCompensation[2],
                    humidity4: (short)((highCompensation[3] << 4) | (highCompensation[4] & 0b1111)),
                    humidity5: (short)(((highCompensation[4] & 0b11110000) >> 4) | (highCompensation[5] << 4)),
                    humidity6: (sbyte)highCompensation[6]);

                var parameters = new CompensationParameters(
                    temperatureCompensation,
                    pressureCompensation,
                    humidityCompensation);

                _logger.LogDebug("Finished loading compensation parameters.");
                _logger.LogTrace(parameters.ToString("\n"));

                return parameters;
            }
        }

        private bool IsMeasurementInProgress()
        {
            Span<byte> config = stackalloc byte[1];
            Read(0xF3, config);

            return (config[0] & 0b100) == 0b100;
        }

        private void Read(byte startAddress, Span<byte> buffer)
        {
            Span<byte> startAddressBytes = stackalloc byte[] { startAddress };
            _bus.Write(_address, startAddressBytes);

            _bus.Read(_address, buffer);
        }

        private void Write(byte address, byte data)
        {
            Span<byte> payload = new[] { address, data };
            _bus.Write(_address, payload);
        }

        public void Dispose()
        {
            _bus.Dispose();
        }

        public enum Oversampling
        {
            None = 0b000,
            X1 = 0b001,
            X2 = 0b010,
            X4 = 0b011,
            X8 = 0b100,
            X16 = 0b101
        }

        public class CompensationParameters
        {
            public TemperatureCompensationParameters Temperature { get; }
            public PressureCompensationParameters Pressure { get; }
            public HumidityCompensationParameters Humidity { get; }

            public CompensationParameters(
                TemperatureCompensationParameters temperature,
                PressureCompensationParameters pressure,
                HumidityCompensationParameters humidity)
            {
                Temperature = temperature;
                Pressure = pressure;
                Humidity = humidity;
            }

            public string ToString(string separator)
            {
                return $"T1: {Temperature.Temperature1:X4}{separator}"
                    + $"T2: {Temperature.Temperature2:X4}{separator}"
                    + $"T3: {Temperature.Temperature3:X4}{separator}"
                    + $"P1: {Pressure.Pressure1:X4}{separator}"
                    + $"P2: {Pressure.Pressure2:X4}{separator}"
                    + $"P3: {Pressure.Pressure3:X4}{separator}"
                    + $"P4: {Pressure.Pressure4:X4}{separator}"
                    + $"P5: {Pressure.Pressure5:X4}{separator}"
                    + $"P6: {Pressure.Pressure6:X4}{separator}"
                    + $"P7: {Pressure.Pressure7:X4}{separator}"
                    + $"P8: {Pressure.Pressure8:X4}{separator}"
                    + $"P9: {Pressure.Pressure9:X4}{separator}"
                    + $"H1: {Humidity.Humidity1:X2}{separator}"
                    + $"H2: {Humidity.Humidity2:X4}{separator}"
                    + $"H3: {Humidity.Humidity3:X2}{separator}"
                    + $"H4: {Humidity.Humidity4:X4}{separator}"
                    + $"H5: {Humidity.Humidity5:X4}{separator}"
                    + $"H6: {Humidity.Humidity6:X2}";
            }

            public override string ToString()
            {
                return ToString(", ");
            }
        }
    }
}
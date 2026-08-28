using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rcm.Common.Temporal;
using Rcm.I2c;
using Rcm.Sensors.Abstractions;

namespace Rcm.Sensors.Bme280;

public sealed class Bme280I2cDevice : ISensor, IDisposable
{
    private static TimeSpan MeasurementDelayTolerance => TimeSpan.FromMilliseconds(20);

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

    public async Task<SensorMeasurement> ReadMeasurementAsync(CancellationToken token)
    {
        _logger.LogDebug("Initiating measurement");
        InitiateMeasurement();

        await WaitForMeasurementCompletionAsync(token);

        _logger.LogDebug("Reading measurement results");
        var (rawPressure, rawTemperature, rawHumidity) = ReadMeasurementResults();

        _logger.LogDebug("Compensating measurement results");
        return CompensateResults(rawPressure, rawTemperature, rawHumidity, _compensationParameters.Value);
    }

    private void InitiateMeasurement()
    {
        const byte humiditySettingsRegisterAddress = 0xF2;
        Write(humiditySettingsRegisterAddress, Oversampling.X8);

        const byte measurementControlRegisterAddress = 0xF4;
        const byte temperatureOversampling = Oversampling.X8;
        const byte pressureOversampling = Oversampling.X16;
        const int forcedMode = 0b10;

        const byte controlValue =
            ((temperatureOversampling << 5) | (pressureOversampling << 2) | forcedMode) & 0xFF;
        Write(measurementControlRegisterAddress, controlValue);
    }

    private async Task WaitForMeasurementCompletionAsync(CancellationToken token)
    {
        var reported = false;
        var stopwatch = Stopwatch.StartNew();

        do
        {
            if (!reported && stopwatch.Elapsed > TimeSpan.FromSeconds(10))
            {
                _logger.LogWarning("Measurement is taking longer than 10 seconds to complete");
                reported = true;
            }

            await Task.Delay(50, token);
        }
        while (IsMeasurementInProgress());

        stopwatch.Stop();

        ReportMeasurementDuration(stopwatch.Elapsed);
    }

    private bool IsMeasurementInProgress()
    {
        const byte measurementDone = 1 << 3;

        Span<byte> config = stackalloc byte[1];
        Read(0xF3, config);

        return (config[0] & measurementDone) == measurementDone;
    }

    private void ReportMeasurementDuration(TimeSpan duration)
    {
        if (duration > TimeSpan.FromMilliseconds(100) + MeasurementDelayTolerance)
        {
            _logger.LogWarning("Measurement took {DurationInMilliseconds}ms", duration.TotalMilliseconds);
        }
        else
        {
            _logger.LogDebug("Measurement took {DurationInMilliseconds}ms", duration.TotalMilliseconds);
        }
    }

    private (int Pressure, int Temperature, int Humidity) ReadMeasurementResults()
    {
        const byte firstMeasurementResultRegisterAddress = 0xF7;
        const byte resultRegistersSize = 0xFE - firstMeasurementResultRegisterAddress + 1;

        Span<byte> results = stackalloc byte[resultRegistersSize];
        Read(firstMeasurementResultRegisterAddress, results);

        var pressure = (results[0] << 12) | (results[1] << 4) | (results[2] >> 4);
        var temperature = (results[3] << 12) | (results[4] << 4) | (results[5] >> 4);
        var humidity = (results[6] << 8) | results[7];

        _logger.LogTrace("Read pressure {Pressure:X5}, temperature {Temperature:X5}, humidity {Humidity:X4}", pressure, temperature, humidity);

        return (pressure, temperature, humidity);
    }

    private SensorMeasurement CompensateResults(
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

        _logger.LogTrace("Compensated values as {Temperature}°C, {Pressure}hPa, {Humidity}%rH", resultingTemperature, pressure, humidity);

        return new()
        {
            Time = _clock.Now,
            CelsiusTemperature = resultingTemperature,
            RelativeHumidity = humidity,
            HpaPressure = pressure
        };
    }

    // The calculation logic in the following method is adapted from BME280 data sheet, chapter 4.2.3 Compensation Formulas
    private static decimal CompensatePressure(
        int rawPressure,
        int fineTemperature,
        PressureCompensationParameters compensation)
    {
        var v1 = fineTemperature - 128000L;
        var v1Squared = v1 * v1;
        var v2 = v1Squared * compensation.Pressure6
            + ((v1 * compensation.Pressure5) << 17)
            + (compensation.Pressure4 << 35);

        v1 = ((v1Squared * compensation.Pressure3) >> 8) + ((v1 * compensation.Pressure2) << 12);
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

    private CompensationParameters ReadCompensationParameters()
    {
        _logger.LogDebug("Loading compensation parameters");

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
            var parameters = new CompensationParameters
            {
                Temperature = new()
                {
                    Temperature1 = (ushort)(lowCompensation[0] | (lowCompensation[1] << 8)),
                    Temperature2 = (short)(lowCompensation[2] | (lowCompensation[3] << 8)),
                    Temperature3 = (short)(lowCompensation[4] | (lowCompensation[5] << 8))
                },
                Humidity = new()
                {
                    Humidity1 = lowCompensation[25],
                    Humidity2 = (short)(highCompensation[0] | (highCompensation[1] << 8)),
                    Humidity3 = highCompensation[2],
                    Humidity4 = (short)((highCompensation[3] << 4) | (highCompensation[4] & 0b1111)),
                    Humidity5 = (short)(((highCompensation[4] & 0b11110000) >> 4) | (highCompensation[5] << 4)),
                    Humidity6 = (sbyte)highCompensation[6]
                },
                Pressure = new()
                {
                    Pressure1 = (ushort)(lowCompensation[6] | (lowCompensation[7] << 8)),
                    Pressure2 = (short)(lowCompensation[8] | (lowCompensation[9] << 8)),
                    Pressure3 = (short)(lowCompensation[10] | (lowCompensation[11] << 8)),
                    Pressure4 = (short)(lowCompensation[12] | (lowCompensation[13] << 8)),
                    Pressure5 = (short)(lowCompensation[14] | (lowCompensation[15] << 8)),
                    Pressure6 = (short)(lowCompensation[16] | (lowCompensation[17] << 8)),
                    Pressure7 = (short)(lowCompensation[18] | (lowCompensation[19] << 8)),
                    Pressure8 = (short)(lowCompensation[20] | (lowCompensation[21] << 8)),
                    Pressure9 = (short)(lowCompensation[22] | (lowCompensation[23] << 8))
                }
            };

            _logger.LogDebug("Finished loading compensation parameters");
            _logger.LogTrace("{ParameterValues}", parameters.Format(separator: "\n"));

            return parameters;
        }
    }

    private void Read(byte startAddress, Span<byte> buffer)
    {
        _bus.Write(_address, [startAddress]);

        _bus.Read(_address, buffer);
    }

    private void Write(byte address, byte data)
    {
        _bus.Write(_address, [address, data]);
    }

    public void Dispose()
    {
        _bus.Dispose();
    }

    private static class Oversampling
    {
        public const byte None = 0b000;
        public const byte X1 = 0b001;
        public const byte X2 = 0b010;
        public const byte X4 = 0b011;
        public const byte X8 = 0b100;
        public const byte X16 = 0b101;
    }

    private class CompensationParameters
    {
        public required TemperatureCompensationParameters Temperature { get; init; }
        public required PressureCompensationParameters Pressure { get; init; }
        public required HumidityCompensationParameters Humidity { get; init; }

        public override string ToString()
        {
            return Format(separator: ", ");
        }

        public string Format(string separator)
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
    }
}

using System;
using NUnit.Framework;
using Rcm.Sensors.Bme280;

namespace Rcm.UnitTests.Sensors.Bme280;

[TestFixture]
public class HumidityCalculatorTests
{
    [Test(Description = "This test is based on real-use data and tests datasheet-based code. Its main purpose is debugging")]
    public void CalculatesHumidityCorrectly()
    {
        // Given
        var rawHumidity = 0x6E32;
        var fineTemperature = 0x19E8C;
        var compensationParameters = new HumidityCompensationParameters
        {
            Humidity1 = 0x4B,
            Humidity2 = 0x169,
            Humidity3 = 0,
            Humidity4 = 0x140,
            Humidity5 = 0x32,
            Humidity6 = 0x1E
        };

        var calculator = new HumidityCalculator(compensationParameters);

        // When
        var humidity = calculator.CalculateHumidity(rawHumidity, fineTemperature);

        // Then
        Assert.AreEqual(42.38m, Math.Round(humidity, 2));
    }
}

using System;
using NUnit.Framework;

namespace Rcm.Sensors.Bme280.UnitTests;

[TestFixture]
public class HumidityCalculatorTests
{
    [Test(Description = "This test is based on real-use data and tests datasheet-based code. Its main purpose is debugging")]
    public void CalculatesHumidityCorrectly()
    {
        // given
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

        // when
        var humidity = calculator.CalculateHumidity(rawHumidity, fineTemperature);

        // then
        Assert.AreEqual(42.38, Math.Round(humidity, 2));
    }
}

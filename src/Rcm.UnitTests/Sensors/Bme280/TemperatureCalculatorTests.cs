using NUnit.Framework;
using Rcm.Sensors.Bme280;

namespace Rcm.UnitTests.Sensors.Bme280;

[TestFixture]
public class TemperatureCalculatorTests
{
    [Test(Description = "This test is based on real-use data and tests datasheet-based code. Its main purpose is debugging")]
    public void CorrectlyCalculatesCompensatedTemperature()
    {
        // Given
        var parameters = new TemperatureCompensationParameters
        {
            Temperature1 = 0x6D86,
            Temperature2 = 0x670C,
            Temperature3 = 0x32
        };

        var calculator = new TemperatureCalculator(parameters);

        var rawTemperature = 0x7D9D4;

        // When
        var (temperature, _) = calculator.CalculateTemperature(rawTemperature);

        // Then
        Assert.AreEqual(20.73m, temperature);
    }
}

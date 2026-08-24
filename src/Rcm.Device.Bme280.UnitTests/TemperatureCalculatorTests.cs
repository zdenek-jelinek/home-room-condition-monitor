using NUnit.Framework;

namespace Rcm.Device.Bme280.UnitTests;

[TestFixture]
public class TemperatureCalculatorTests
{
    [Test(Description = "This test is based on real-use data and tests datasheet-based code. Its main purpose is debugging")]
    public void CorrectlyCalculatesCompensatedTemperature()
    {
        // given
        var parameters = new TemperatureCompensationParameters(0x6D86, 0x670C, 0x32);

        var calculator = new TemperatureCalculator(parameters);

        var rawTemperature = 0x7D9D4;

        // when
        var (temperature, _) = calculator.CalculateTemperature(rawTemperature);

        // then
        Assert.AreEqual(20.73m, temperature);
    }
}
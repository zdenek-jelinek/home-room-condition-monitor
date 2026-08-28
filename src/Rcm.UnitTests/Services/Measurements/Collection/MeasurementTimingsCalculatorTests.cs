using System;
using NUnit.Framework;
using Rcm.Services.Measurements.Collection;
using Rcm.Testing.Common.Temporal;

namespace Rcm.UnitTests.Services.Measurements.Collection;

[TestFixture]
public class MeasurementTimingsCalculatorTests
{
    [Test]
    public void MeasurementStartsOnNextMinuteWithSixSecondsPeriodForNonZeroSecondsTime()
    {
        // Given
        var nonZeroSecondsTime = new DateTimeOffset(2018, 12, 27, 13, 28, 10, TimeSpan.Zero);

        // When
        var timings = CalculateMeasurementTimings(nonZeroSecondsTime);

        // Then
        Assert.AreEqual(TimeSpan.FromSeconds(60 - nonZeroSecondsTime.Second), timings.InitialDelay);
        Assert.AreEqual(TimeSpan.FromSeconds(6), timings.Period);
    }

    [Test]
    public void MeasurementStartsNowWithSixSecondsPeriodForZeroSecondsTime()
    {
        // Given
        var zeroSecondsTime = new DateTimeOffset(2018, 12, 27, 13, 28, 0, TimeSpan.Zero);

        // When
        var timings = CalculateMeasurementTimings(zeroSecondsTime);

        // Then
        Assert.AreEqual(TimeSpan.Zero, timings.InitialDelay);
        Assert.AreEqual(TimeSpan.FromSeconds(6), timings.Period);
    }

    private static MeasurementCollectionTimings CalculateMeasurementTimings(DateTimeOffset currentTime)
    {
        var timingsCalculator = new MeasurementTimingsCalculator(new FixedClock { Now = currentTime });

        return timingsCalculator.DetermineMeasurementTimings();
    }
}

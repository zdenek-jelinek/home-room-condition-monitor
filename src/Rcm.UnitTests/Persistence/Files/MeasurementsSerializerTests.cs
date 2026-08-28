using System;
using NUnit.Framework;
using Rcm.Common;
using Rcm.Common.Temporal;
using Rcm.Persistence.Files;

namespace Rcm.UnitTests.Persistence.Files;

[TestFixture]
public class MeasurementsSerializerTests
{
    private const string CultureThatUsesCommaAsDecimalSeparator = "cs-CZ";

    [Test]
    public void SerializesEntryAsHoursAndMinutesThenOffsetHoursAndMinutesThenTemperatureThenHumidityThenPressureAllSeparatedBySpaces()
    {
        // Given
        var measurement = new MeasurementEntry
        {
            Time = new DateTimeOffset(2018, 12, 30, 19, 50, 10, offset: TimeSpan.FromHours(-1.5)),
            CelsiusTemperature = 32m,
            HpaPressure = 980m,
            RelativeHumidity = 52m
        };

        var serializer = new MeasurementsSerializer();

        // When
        var text = serializer.Serialize(measurement);

        // Then
        Assert.AreEqual("19:50-01:30 32 52 980", text);
    }

    [Test]
    [SetCulture(CultureThatUsesCommaAsDecimalSeparator)]
    public void UsesInvariantCultureToSerializeEntries()
    {
        // Given
        var entry = new MeasurementEntry
        {
            Time = new DateTimeOffset(2018, 12, 30, 19, 50, 10, offset: TimeSpan.FromHours(-1.5)),
            CelsiusTemperature = 32.3m,
            RelativeHumidity = 52.5m,
            HpaPressure = 980.93m
        };

        var serializer = new MeasurementsSerializer();

        // When
        var text = serializer.Serialize(entry);

        // Then
        Assert.AreEqual("19:50-01:30 32.3 52.5 980.93", text);
    }

    [Test]
    public void DeserializesEntryFromRecordComposedOfTimeAndOffsetAndTemperatureAndHumidityAndPressureAllSeparatedBySpaces()
    {
        // Given
        var time = new DateTimeOffset(2018, 12, 30, 20, 50, 0, TimeSpan.FromHours(2));
        var record = "20:50+02:00 35 48 1010";

        var serializer = new MeasurementsSerializer();

        // When
        var entry = serializer.Deserialize(time.ToDateOnly(), record);

        // Then
        Assert.AreEqual(time, entry.Time);
        Assert.AreEqual(35m, entry.CelsiusTemperature);
        Assert.AreEqual(48m, entry.RelativeHumidity);
        Assert.AreEqual(1010m, entry.HpaPressure);
    }

    [Test]
    [SetCulture(CultureThatUsesCommaAsDecimalSeparator)]
    public void UsesInvariantCultureToDeserializeEntries()
    {
        // Given
        var time = new DateTimeOffset(2018, 12, 30, 20, 50, 0, TimeSpan.FromHours(2));
        var record = "20:50+02:00 35.0 48.2 1010.8";

        var serializer = new MeasurementsSerializer();

        // When
        var entry = serializer.Deserialize(time.ToDateOnly(), record);

        // Then
        Assert.AreEqual(time, entry.Time);
        Assert.AreEqual(35.0m, entry.CelsiusTemperature);
        Assert.AreEqual(48.2m, entry.RelativeHumidity);
        Assert.AreEqual(1010.8m, entry.HpaPressure);
    }

    [Test]
    [TestCase("")]
    [TestCase("12:00+1:00", Description = "No measurement data except time")]
    [TestCase("12:00+1:00 17.5", Description = "No humidity and pressure data")]
    [TestCase("12:00+1:00 17.5 42.1", Description = "No pressure data")]
    [TestCase("12:00+1:00 17.5 42.1 975.2 unexpected", Description = "Unexpected data after pressure")]
    [TestCase("12:00+1:00 17.5 42.1 975.2 ", Description = "Unexpected whitespace after pressure")]
    [TestCase("Invalid 17.5 42.1. 975.2", Description = "Invalid datetime format")]
    [TestCase("12:00+1:00 Invalid 42.1. 975.2", Description = "Invalid temperature format")]
    [TestCase("12:00+1:00 17.5 Invalid. 975.2", Description = "Invalid humidity format")]
    [TestCase("12:00+1:00 17.5 42.1. Invalid", Description = "Invalid pressure format")]
    [TestCase("12:00+1:00  17.5 42.1 975.2", Description = "Unexpected whitespace between time and temperature")]
    [TestCase("12:00+1:00 17.5  42.1 975.2", Description = "Unexpected whitespace between temperature and humidity")]
    public void ThrowsForInvalidRecords(string invalidRecord)
    {
        // Given
        var dummyDate = new DateOnly(2018, 12, 31);

        var serializer = new MeasurementsSerializer();

        // When
        void DeserializingInvalidRecord() => serializer.Deserialize(dummyDate, invalidRecord);

        // Then
        _ = Assert.Catch(DeserializingInvalidRecord);
    }
}

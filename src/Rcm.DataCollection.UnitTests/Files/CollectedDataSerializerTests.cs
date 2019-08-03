using System;
using NUnit.Framework;
using Rcm.Common;
using Rcm.DataCollection.Files;

namespace Rcm.DataCollection.UnitTests.Files
{
    [TestFixture]
    public class CollectedDataSerializerTests
    {
        private const string CultureThatUsesCommaAsDecimalSeparator = "cs-CZ";

        [Test]
        public void SerializesEntryAsHoursAndMinutesThenOffsetHoursAndMinutesThenTemperatureThenHumidityThenPressureAllSeparatedBySpaces()
        {
            // given
            var offset = TimeSpan.FromHours(-1) + TimeSpan.FromMinutes(-30);
            var time = new DateTimeOffset(2018, 12, 30, 19, 50, 10, offset);
            var temperature = 32m;
            var humidity = 52m;
            var pressure = 980m;
            var entry = new MeasurementEntry(time, temperature, humidity, pressure);

            var serializer = new CollectedDataSerializer();

            // when
            var record = serializer.Serialize(entry);

            // then
            Assert.AreEqual("19:50-01:30 32 52 980", record);
        }

        [Test]
        [SetCulture(CultureThatUsesCommaAsDecimalSeparator)]
        public void UsesInvariantCultureToSerializeEntries()
        {
            // given
            var offset = TimeSpan.FromHours(-1) + TimeSpan.FromMinutes(-30);
            var time = new DateTimeOffset(2018, 12, 30, 19, 50, 10, offset);
            var temperature = 32.3m;
            var humidity = 52.5m;
            var pressure = 980.93m;
            var entry = new MeasurementEntry(time, temperature, humidity, pressure);

            var serializer = new CollectedDataSerializer();

            // when
            var record = serializer.Serialize(entry);

            // then
            Assert.AreEqual("19:50-01:30 32.3 52.5 980.93", record);
        }

        [Test]
        public void DeserializesEntryFromRecordComposedOfTimeAndOffsetAndTemperatureAndHumidityAndPressureAllSeparatedBySpaces()
        {
            // given
            var time = new DateTimeOffset(2018, 12, 30, 20, 50, 0, TimeSpan.FromHours(2));
            var record = "20:50+02:00 35 48 1010";

            var serializer = new CollectedDataSerializer();

            // when
            var entry = serializer.Deserialize(time.Date, record);

            // then
            Assert.AreEqual(time, entry.Time);
            Assert.AreEqual(35m, entry.CelsiusTemperature);
            Assert.AreEqual(48m, entry.RelativeHumidity);
            Assert.AreEqual(1010m, entry.HpaPressure);
        }

        [Test]
        [SetCulture(CultureThatUsesCommaAsDecimalSeparator)]
        public void UsesInvariantCultureToDeserializeEntries()
        {
            // given
            var time = new DateTimeOffset(2018, 12, 30, 20, 50, 0, TimeSpan.FromHours(2));
            var record = "20:50+02:00 35.0 48.2 1010.8";

            var serializer = new CollectedDataSerializer();

            // when
            var entry = serializer.Deserialize(time.Date, record);

            // then
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
            // given
            var dummyDate = new DateTime(2018, 12, 31, 0, 0, 0);

            var serializer = new CollectedDataSerializer();

            // when
            void DeserializingInvalidRecord() => serializer.Deserialize(dummyDate, invalidRecord);

            // then
            Assert.Catch(DeserializingInvalidRecord);
        }
    }
}

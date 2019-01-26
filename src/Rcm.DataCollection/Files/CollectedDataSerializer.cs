using System;
using System.Globalization;
using Rcm.Common;

namespace Rcm.DataCollection.Files
{
    public class CollectedDataSerializer
    {
        public string Serialize(MeasurementEntry entry)
        {
            return $@"{entry.Time:HH\:mmK} {entry.CelsiusTemperature} {entry.RelativeHumidity} {entry.HpaPressure}";
        }

        public MeasurementEntry Deserialize(DateTime date, ReadOnlySpan<char> record)
        {
            var offset = 0;
            var time = ParseTime(record, ref offset);
            ConsumeSpace(record, ref offset);
            var temperature = ParseDecimal(record, ref offset);
            ConsumeSpace(record, ref offset);
            var humidity = ParseDecimal(record, ref offset);
            ConsumeSpace(record, ref offset);
            var pressure = ParseDecimal(record, ref offset);
            EnsureEnd(record, offset);

            return new MeasurementEntry(
                new DateTimeOffset(date.Year, date.Month, date.Day, time.Hour, time.Minute, second: 0, time.Offset),
                temperature,
                humidity,
                pressure);
        }

        private void EnsureEnd(ReadOnlySpan<char> record, int offset)
        {
            if (record.Length != offset)
            {
                throw new FormatException($"Record has invalid length {record.Length}, expected end at offset {offset}");
            }
        }

        private decimal ParseDecimal(ReadOnlySpan<char> record, ref int offset)
        {
            var nextSpaceIndex = record.Slice(offset).IndexOf(' ');
            if (nextSpaceIndex < 0)
            {
                var result = Decimal.Parse(record.Slice(offset));
                
                offset = record.Length;
                return result;
            }
            else
            {
                var result = Decimal.Parse(record.Slice(offset, nextSpaceIndex));
                
                offset += nextSpaceIndex;
                return result;
            }
        }

        private void ConsumeSpace(ReadOnlySpan<char> record, ref int offset)
        {
            if (record[offset] != ' ')
            {
                throw new FormatException($"Invalid separator in record {new String(record)} at offset {offset}");
            }

            offset += 1;
        }

        private DateTimeOffset ParseTime(ReadOnlySpan<char> record, ref int offset)
        {
            var separatorIndex = record.Slice(offset).IndexOf(" ");
            if (separatorIndex < 0)
            {
                throw new FormatException($"Invalid record: Space expected after time entry in \"{new String(record)}\"");
            }

            var time = record.Slice(offset, separatorIndex);

            var result = DateTimeOffset.ParseExact(time, "HH:mmK", CultureInfo.InvariantCulture.DateTimeFormat);

            offset += separatorIndex;

            return result;
        }

        private (int offsetStart, int temperatureStart, int humidityStart, int pressureStart) GetOffsets(ReadOnlySpan<char> record)
        {
            int? offsetStart = null;
            int? temperatureStart = null;
            int? humidityStart = null;
            int? pressureStart = null;

            for (var i = 0; i < record.Length; ++i)
            {
                if (!Char.IsWhiteSpace(record[i]))
                {
                    continue;
                }

                if (offsetStart is null)
                {
                    offsetStart = i + 1;
                }
                else if (temperatureStart is null)
                {
                    temperatureStart = i + 1;
                }
                else if (humidityStart is null)
                {
                    humidityStart = i + 1;
                }
                else
                {
                    pressureStart = i + 1;
                    break;
                }
            }

            if (offsetStart is null
                || temperatureStart is null
                || humidityStart is null
                || pressureStart is null
                || pressureStart >= record.Length)
            {
                throw new ParseException("Given record has invalid format");
            }

            return (offsetStart.Value, temperatureStart.Value, humidityStart.Value, pressureStart.Value);
        }

        public class ParseException : Exception
        {
            public ParseException(string message) : base(message)
            {
            }
        }
    }
}

using System;
using Rcm.Common;
using static System.Globalization.CultureInfo;

namespace Rcm.Persistence.Files;

public class MeasurementsSerializer
{
    public string Serialize(MeasurementEntry entry)
    {
        var temperature = entry.CelsiusTemperature.ToString(InvariantCulture);
        var relativeHumidity = entry.RelativeHumidity.ToString(InvariantCulture);
        var pressure = entry.HpaPressure.ToString(InvariantCulture);

        return $@"{entry.Time:HH\:mmK} {temperature} {relativeHumidity} {pressure}";
    }

    public MeasurementEntry Deserialize(DateOnly date, ReadOnlySpan<char> record)
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

        return new()
        {
            Time = new DateTimeOffset(date, new(time.Hour, time.Minute, second: 0), time.Offset),
            CelsiusTemperature = temperature,
            RelativeHumidity = humidity,
            HpaPressure = pressure
        };
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
            var result = Decimal.Parse(record.Slice(offset), provider: InvariantCulture);

            offset = record.Length;
            return result;
        }
        else
        {
            var result = Decimal.Parse(record.Slice(offset, nextSpaceIndex), provider: InvariantCulture);

            offset += nextSpaceIndex;
            return result;
        }
    }

    private void ConsumeSpace(ReadOnlySpan<char> record, ref int offset)
    {
        if (record[offset] != ' ')
        {
            throw new FormatException($"Invalid separator in record {record} at offset {offset}");
        }

        offset += 1;
    }

    private DateTimeOffset ParseTime(ReadOnlySpan<char> record, ref int offset)
    {
        var separatorIndex = record.Slice(offset).IndexOf(" ");
        if (separatorIndex < 0)
        {
            throw new FormatException($"Invalid record: Space expected after time entry in \"{record}\"");
        }

        var time = record.Slice(offset, separatorIndex);

        var result = DateTimeOffset.ParseExact(time, "HH:mmK", InvariantCulture.DateTimeFormat);

        offset += separatorIndex;

        return result;
    }
}

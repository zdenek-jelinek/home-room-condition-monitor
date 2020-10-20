using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;
using Rcm.Common;
using static System.Globalization.CultureInfo;
using static Microsoft.WindowsAzure.Storage.Table.QueryComparisons;
using static Microsoft.WindowsAzure.Storage.Table.TableOperators;
using static Microsoft.WindowsAzure.Storage.Table.TableQuery;

namespace Rcm.Backend.Persistence.Measurements
{
    public class MeasurementsGateway : IMeasurementsWriter, IMeasurementsReader
    {
        private readonly CloudTable _table;

        public MeasurementsGateway(CloudTable table) => _table = table;

        public async Task StoreAsync(DeviceMeasurements measurements, CancellationToken token)
        {
            await _table.CreateIfNotExistsAsync(default, default, token);

            await Task.WhenAll(measurements.Measurements.Select(m => StoreAsync(measurements.DeviceIdentifier, m, token)));
        }

        private Task StoreAsync(string deviceId, Measurement measurement, CancellationToken token)
        {
            var entity = ToEntity(deviceId, measurement);

            return _table.ExecuteAsync(TableOperation.InsertOrReplace(entity), default, default, token);
        }

        private static DeviceMeasurementTableEntity ToEntity(string deviceId, Measurement measurement)
        {
            return new DeviceMeasurementTableEntity
            {
                PartitionKey = ComposePartitionKey(deviceId, measurement.Time),
                RowKey = ComposeRowKey(measurement.Time),
                Humidity = measurement.Humidity.ToString(InvariantCulture),
                Temperature = measurement.Temperature.ToString(InvariantCulture),
                Pressure = measurement.Pressure.ToString(InvariantCulture)
            };
        }

        public async IAsyncEnumerable<Measurement> GetMeasurementsAsync(
            string deviceId,
            DateTime start,
            DateTime end,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            start = start.ToUniversalTime();
            end = end.ToUniversalTime();

            var query = BuildQuery(deviceId, start, end);

            var segment = (TableQuerySegment<DeviceMeasurementTableEntity>?)null;
            do
            {
                segment = await _table.ExecuteQuerySegmentedAsync(query, segment?.ContinuationToken, default, default, cancellationToken);
                foreach (var entity in segment)
                {
                    yield return FromEntity(entity);
                }
            }
            while (segment.ContinuationToken != null);
        }

        private static TableQuery<DeviceMeasurementTableEntity> BuildQuery(string deviceId, DateTime start, DateTime end)
        {
            return new TableQuery<DeviceMeasurementTableEntity>
            {
                FilterString = BuildFilter(deviceId, start, end)
            };
        }

        private static string BuildFilter(string deviceId, DateTime start, DateTime end)
        {
            if (start.Date.Equals(end.Date))
            {
                return CombineFilters(
                    GenerateFilterCondition(nameof(TableEntity.PartitionKey), Equal, ComposePartitionKey(deviceId, start)),
                    And,
                    CombineFilters(
                        GenerateFilterCondition(nameof(TableEntity.RowKey), GreaterThanOrEqual, ComposeRowKey(start)),
                        And,
                        GenerateFilterCondition(nameof(TableEntity.RowKey), LessThanOrEqual, ComposeRowKey(end))));
            }
            else
            {
                var partitionStart = ComposePartitionKey(deviceId, start);
                var partitionEnd = ComposePartitionKey(deviceId, end);

                return CombineFilters(
                    CombineFilters(
                        GenerateFilterCondition(nameof(TableEntity.PartitionKey), Equal, partitionStart),
                        And,
                        GenerateFilterCondition(nameof(TableEntity.RowKey), GreaterThanOrEqual, ComposeRowKey(start))),
                    Or,
                    CombineFilters(
                        CombineFilters(
                            GenerateFilterCondition(nameof(TableEntity.PartitionKey), GreaterThan, partitionStart),
                            And,
                            GenerateFilterCondition(nameof(TableEntity.PartitionKey), LessThan, partitionEnd)),
                        Or,
                        CombineFilters(
                            GenerateFilterCondition(nameof(TableEntity.PartitionKey), Equal, partitionEnd),
                            And,
                            GenerateFilterCondition(nameof(TableEntity.RowKey), LessThanOrEqual, ComposeRowKey(end)))));
            }
        }

        private static string ComposeRowKey(DateTime time)
        {
            return time.ToString(DateTimeFormat.Iso8601Time, InvariantCulture);
        }

        private static string ComposePartitionKey(string deviceId, DateTime date)
        {
            return $"{deviceId}_{date.ToString(DateTimeFormat.Iso8601Date, InvariantCulture)}";
        }

        private static Measurement FromEntity(DeviceMeasurementTableEntity entity)
        {
            var dateString = entity.PartitionKey.Substring(entity.PartitionKey.LastIndexOf('_'));
            var date = DateTime.ParseExact(dateString, DateTimeFormat.Iso8601Date, InvariantCulture, DateTimeStyles.AssumeUniversal);
            var time = DateTime.ParseExact(entity.RowKey, DateTimeFormat.Iso8601Time, InvariantCulture, DateTimeStyles.AssumeUniversal);

            return new Measurement(
                time: new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second, time.Millisecond, DateTimeKind.Utc),
                temperature: Decimal.Parse(entity.Temperature!, InvariantCulture),
                pressure: Decimal.Parse(entity.Pressure!, InvariantCulture),
                humidity: Decimal.Parse(entity.Humidity!, InvariantCulture));
        }

        private class DeviceMeasurementTableEntity : TableEntity
        {
            public string? Temperature { get; set; }
            public string? Humidity { get; set; }
            public string? Pressure { get; set; }
        }
    }
}

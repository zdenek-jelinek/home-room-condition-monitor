using System;
using System.IO;
using Rcm.Device.Common;
using static System.Globalization.CultureInfo;
using static System.Globalization.DateTimeStyles;
using static Rcm.Common.DateTimeFormat;

namespace Rcm.Connector.Configuration
{
    public class FileLatestUploadedMeasurementGateway
        : ILatestUploadedMeasurementReader, ILatestUploadedMeasurementWriter
    {
        private readonly IDataStorageLocation _dataStorageLocation;

        private string LatestUploadedMeasurementFilePath
        {
            get => Path.Combine(_dataStorageLocation.Path, "latest.txt");
        }

        public FileLatestUploadedMeasurementGateway(IDataStorageLocation dataStorageLocation)
        {
            _dataStorageLocation = dataStorageLocation;
        }

        public DateTimeOffset? GetLatestUploadedMeasurementTime()
        {
            try
            {
                var latestUploadedMeasurementFileContents = File.ReadAllText(LatestUploadedMeasurementFilePath);
                return ParseIsoDateTime(latestUploadedMeasurementFileContents);
            }
            catch
            {
                return null;
            }
        }

        private static DateTimeOffset? ParseIsoDateTime(string timeString)
        {
            if (DateTimeOffset.TryParseExact(timeString, Iso8601DateTime, InvariantCulture, None, out var time))
            {
                return time;
            }
            else
            {
                return null;
            }
        }

        public void SetLatestMeasurementUploadTime(DateTimeOffset? time)
        {
            if (IsOlderThanCurrentlyStoredTime(time))
            {
                return;
            }

            var timeString = time?.ToString(Iso8601DateTime, InvariantCulture) ?? String.Empty;

            StoreTime(timeString);
        }

        private void StoreTime(string timeString)
        {
            try
            {
                File.WriteAllText(LatestUploadedMeasurementFilePath, timeString);
            }
            catch (DirectoryNotFoundException)
            {
                Directory.CreateDirectory(_dataStorageLocation.Path);
                File.WriteAllText(LatestUploadedMeasurementFilePath, timeString);
            }
        }

        private bool IsOlderThanCurrentlyStoredTime(DateTimeOffset? time)
        {
            if (time == null)
            {
                return false;
            }

            var storedTime = GetLatestUploadedMeasurementTime();
            if (storedTime != null && storedTime > time)
            {
                return true;
            }

            return false;
        }
    }
}

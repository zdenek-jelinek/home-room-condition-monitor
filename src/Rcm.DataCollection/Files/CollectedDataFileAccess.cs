using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rcm.Common;
using Rcm.Common.IO;

namespace Rcm.DataCollection.Files
{
    public class CollectedDataFileAccess : ICollectedDataFileAccess
    {
        private readonly ILogger<CollectedDataFileAccess> _logger;
        private readonly IFileAccess _file;

        private readonly CollectedDataFilesNavigator _filesNavigator;
        private readonly CollectedDataSerializer _serializer;

        public CollectedDataFileAccess(
            ILogger<CollectedDataFileAccess> logger,
            IDataStorageLocation dataStorageLocation,
            IFileAccess file)
        {
            _logger = logger;
            _file = file;
            _filesNavigator = new CollectedDataFilesNavigator(dataStorageLocation);
            _serializer = new CollectedDataSerializer();
        }

        public IEnumerable<MeasurementEntry> Read(DateTimeOffset start, DateTimeOffset end)
        {
            foreach (var (date, path) in _filesNavigator.GetFilePaths(start, end))
            {
                if (!_file.TryOpenText(path, out var file, CannotOpenFile))
                {
                    continue;
                }

                using (file)
                {
#pragma warning disable CS8602 // Possible dereference of a null reference.
                    while (!file.EndOfStream)
#pragma warning restore CS8602 // Possible dereference of a null reference.
                    {
                        var line = file.ReadLine();
                        var entry = _serializer.Deserialize(date, line);

                        if (entry.Time >= start && entry.Time <= end)
                        {
                            yield return entry;
                        }
                    }
                }
            }
        }

        private void CannotOpenFile(string path, Exception e)
        {
            _logger.LogWarning($"Could not open measurements file \"{path}\" for reading", e);
        }

        public async Task SaveAsync(MeasurementEntry entry)
        {
            var record = _serializer.Serialize(entry);
            var path = _filesNavigator.GetFilePath(entry.Time);

            using (var file = _file.AppendText(path))
            {
                await file.WriteLineAsync(record);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rcm.Common;
using Rcm.Common.IO;
using Rcm.Device.Common;

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
                using var file = _file.OpenText(path, CannotOpenFile);
                if (file is null)
                {
                    _logger.LogTrace($"Skipping read from \"{Path.GetFullPath(path)}\" as the file does not exist");
                    continue;
                }

                for (var lineNumber = 1; !file.EndOfStream; ++lineNumber)
                {
                    var line = file.ReadLine();

                    var entry = TryParseEntry(date, path, lineNumber, line);
                    if (entry != null && entry.Time >= start && entry.Time <= end)
                    {
                        _logger.LogTrace($"Read record of {entry} from \"{Path.GetFullPath(path)}\"");
                        yield return entry;
                    }
                }
            }
        }

        private MeasurementEntry? TryParseEntry(DateTime date, string filePath, int lineNumber, string line)
        {
            try
            {
                return _serializer.Deserialize(date, line);
            }
            catch (FormatException e)
            {
                _logger.LogWarning(e, $"Skipping corrupt entry in \"{filePath}\", line: {lineNumber}, value: \"{line}\".");
                return default;
            }
        }

        private void CannotOpenFile(string path, Exception e)
        {
            _logger.LogWarning($"Could not open measurements file \"{path}\"(\"{Path.GetFullPath(path)}\") for reading", e);
        }

        public async Task SaveAsync(MeasurementEntry entry)
        {
            var record = _serializer.Serialize(entry);
            var path = _filesNavigator.GetFilePath(entry.Time);
            EnsureDirectoryExists(path);

            _logger.LogTrace($"Storing record of {entry} to \"{Path.GetFullPath(path)}\"");

            using var file = _file.AppendText(path);

            await file.WriteLineAsync(record);
        }

        private void EnsureDirectoryExists(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!String.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                _ = Directory.CreateDirectory(directory);
            }
        }
    }
}
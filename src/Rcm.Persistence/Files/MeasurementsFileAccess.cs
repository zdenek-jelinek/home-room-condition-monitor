using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rcm.Common;
using Rcm.Common.IO;
using Rcm.Persistence.Files.Navigation;

namespace Rcm.Persistence.Files;

public class MeasurementsFileAccess : IMeasurementsFileAccess
{
    private readonly ILogger<MeasurementsFileAccess> _logger;
    private readonly IFileAccess _file;

    private readonly MeasurementsFilesNavigator _filesNavigator;
    private readonly MeasurementsSerializer _serializer;

    public MeasurementsFileAccess(
        ILogger<MeasurementsFileAccess> logger,
        IDataStorageLocation dataStorageLocation,
        IFileAccess file)
    {
        _logger = logger;
        _file = file;
        _filesNavigator = new MeasurementsFilesNavigator(dataStorageLocation);
        _serializer = new MeasurementsSerializer();
    }

    public IEnumerable<MeasurementEntry> Read(DateTimeOffset start, DateTimeOffset end, CancellationToken token)
    {
        foreach (var (date, path) in _filesNavigator.GetFilePaths(start, end))
        {
            token.ThrowIfCancellationRequested();

            var fullFilePath = Path.GetFullPath(path);

            using var file = _file.OpenText(path, CannotOpenFile);
            if (file is null)
            {
                _logger.LogTrace("Skipping read from '{FullSourceFilePath}', the file does not exist", fullFilePath);
                continue;
            }

            for (var lineNumber = 1; !file.EndOfStream; ++lineNumber)
            {
                token.ThrowIfCancellationRequested();

                var line = file.ReadLine();
                if (line == null)
                {
                    // End of file
                    break;
                }

                var entry = TryParseEntry(date, fullFilePath, lineNumber, line);
                if (entry != null && entry.Time >= start && entry.Time <= end)
                {
                    _logger.LogTrace("Read record {Record} from '{FullSourceFilePath}'", entry, fullFilePath);
                    yield return entry;
                }
            }
        }
    }

    private MeasurementEntry? TryParseEntry(DateOnly date, string filePath, int lineNumber, string line)
    {
        try
        {
            return _serializer.Deserialize(date, line);
        }
        catch (FormatException e)
        {
            _logger.LogWarning(e, "Skipping corrupt entry in '{FullSourceFilePath}', line: {LineNumber}, value: '{LineText}'", filePath, lineNumber, line);
            return null;
        }
    }

    private void CannotOpenFile(string path, Exception e)
    {
        _logger.LogWarning(e, "Could not open measurements file '{Path}' ('{FullPath}') for reading", path, Path.GetFullPath(path));
    }

    public async Task SaveAsync(MeasurementEntry entry, CancellationToken token)
    {
        var record = _serializer.Serialize(entry);
        var path = _filesNavigator.GetFilePath(entry.Time);
        EnsureDirectoryExists(path);

        _logger.LogTrace("Storing record {Record} to '{FullDestinationPath}'", entry, Path.GetFullPath(path));

        await using var file = _file.AppendText(path);

        token.ThrowIfCancellationRequested();

        // Do not interrupt write/flush with cancellation so that the file does not get corrupted
        await file.WriteLineAsync(record.AsMemory(), CancellationToken.None);
        await file.FlushAsync(CancellationToken.None);
    }

    private static void EnsureDirectoryExists(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!String.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            _ = Directory.CreateDirectory(directory);
        }
    }
}

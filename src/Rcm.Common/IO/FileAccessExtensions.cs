using System;
using System.Collections.Generic;
using System.IO;
using Rcm.Common.Temporary;

namespace Rcm.Common.IO
{
    public static class FileAccessExtensions
    {
        public static bool TryOpenText(
            this IFileAccess file,
            string path,
            // TODO: Make nullable
            [NotNullWhenTrue] out StreamReader reader,
            // TODO: Make nullable
            Action<string, Exception> exceptionHandler = null)
        {
            try
            {
                if (file.Exists(path))
                {
                    var stream = file.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    reader = new StreamReader(stream);
                    return true;
                }
            }
            catch (Exception e)
            {
                exceptionHandler?.Invoke(path, e);
            }

            reader = default;
            return false;
        }

        public static StreamWriter AppendText(this IFileAccess file, string path)
        {
            var fileStream = file.Open(path, FileMode.Append, FileAccess.Write, FileShare.Read);
            return new StreamWriter(fileStream);
        }

        public static string ReadAllText(this IFileAccess file, string path)
        {
            using (var stream = file.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public static void WriteAllLines(this IFileAccess file, string path, IEnumerable<string> lines)
        {
            using (var stream = file.Open(path, FileMode.Open, FileAccess.Write, FileShare.Read))
            {
                using (var writer = new StreamWriter(stream))
                {

                    foreach (var line in lines)
                    {
                        writer.WriteLine(line);
                    }
                }
            }
        }
    }
}
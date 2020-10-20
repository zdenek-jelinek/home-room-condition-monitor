using System;
using System.Collections.Generic;
using System.IO;

namespace Rcm.Common.IO
{
    public static class FileAccessExtensions
    {
        public static StreamReader? OpenText(
            this IFileAccess file,
            string path,
            Action<string, Exception>? exceptionHandler = null)
        {
            try
            {
                if (file.Exists(path))
                {
                    var stream = file.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    return new StreamReader(stream);
                }
            }
            catch (Exception e)
            {
                exceptionHandler?.Invoke(path, e);
            }

            return null;
        }

        public static StreamWriter AppendText(this IFileAccess file, string path)
        {
            var fileStream = file.Open(path, FileMode.Append, FileAccess.Write, FileShare.Read);
            return new StreamWriter(fileStream);
        }

        public static string ReadAllText(this IFileAccess file, string path)
        {
            var stream = file.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            
            using var reader = new StreamReader(stream);
            
            return reader.ReadToEnd();
        }

        public static void WriteAllLines(this IFileAccess file, string path, IEnumerable<string> lines)
        {
            var stream = file.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);
            
            using var writer = new StreamWriter(stream);

            foreach (var line in lines)
            {
                writer.WriteLine(line);
            }
        }

        public static void WriteAllText(this IFileAccess file, string path, string text)
        {
            var stream = file.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);

            using var writer = new StreamWriter(stream);

            writer.Write(text);
        }
    }
}
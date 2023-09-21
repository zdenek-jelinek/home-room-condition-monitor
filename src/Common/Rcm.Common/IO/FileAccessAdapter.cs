using System.IO;

namespace Rcm.Common.IO;

public class FileAccessAdapter : IFileAccess
{
    public bool Exists(string path) =>
        File.Exists(path);

    public Stream Open(string path, FileMode mode, FileAccess access, FileShare share) =>
        File.Open(path, mode, access, share);
}
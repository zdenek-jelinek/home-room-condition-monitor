using System.IO;

namespace Rcm.Common.IO;

public interface IFileAccess
{
    bool Exists(string path);

    Stream Open(string path, FileMode mode, FileAccess access, FileShare share);
}
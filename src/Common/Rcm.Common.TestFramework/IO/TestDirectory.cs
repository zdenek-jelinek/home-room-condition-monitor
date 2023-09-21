using System.IO;
using System.Threading;

namespace Rcm.Common.TestFramework.IO;

public class TestDirectory
{
    public string Path { get; }

    public TestDirectory(string path)
    {
        Path = path;
    }

    /// <summary>
    /// Creates a new test directory. If it existed, the original directory is deleted and replaced with a new one.
    /// </summary>
    public void PrepareClean()
    {
        Delete();
        Directory.CreateDirectory(Path);
    }

    /// <summary>
    /// Deletes the test directory and blocks the thread until the deletion is written through the filesystem or
    /// until ~250ms have elapsed.
    /// </summary>
    public void Delete()
    {
        if (!Directory.Exists(Path))
        {
            return;
        }

        Directory.Delete(Path, true);
            
        for (var i = 0; i < 25; ++i)
        {
            if (!Directory.Exists(Path))
            {
                return;
            }

            Thread.Sleep(10);
        }
    }
}
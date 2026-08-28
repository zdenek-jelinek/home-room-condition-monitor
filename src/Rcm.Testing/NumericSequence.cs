using System.Threading;

namespace Rcm.Testing;

public class NumericSequence
{
    private static long _number;

    public static long Next()
    {
        return Interlocked.Increment(ref _number);
    }
}

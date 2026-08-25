using System;
using Rcm.Common.Temporal;

namespace Rcm.Testing.Common.Temporal;

public class FixedClock : IClock
{
    public DateTimeOffset Now { get; }

    public FixedClock(DateTimeOffset time)
    {
        Now = time;
    }
}
using System;

namespace Rcm.Common.TestDoubles;

public class FixedClock : IClock
{
    public DateTimeOffset Now { get; }

    public FixedClock(DateTimeOffset time)
    {
        Now = time;
    }
}
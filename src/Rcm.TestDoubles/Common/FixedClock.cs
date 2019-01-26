using System;
using Rcm.Common;

namespace Rcm.TestDoubles.Common
{
    public class FixedClock : IClock
    {
        public DateTimeOffset Now { get; }

        public FixedClock(DateTimeOffset time)
        {
            Now = time;
        }
    }
}
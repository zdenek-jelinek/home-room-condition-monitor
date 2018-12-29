using System;
using Rcm.Common;

namespace Rcm.DataCollection.UnitTests
{
    internal class FixedClock : IClock
    {
        public DateTimeOffset Now { get; }

        public FixedClock(DateTimeOffset time)
        {
            Now = time;
        }
    }
}
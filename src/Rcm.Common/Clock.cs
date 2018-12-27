using System;

namespace Rcm.Common
{
    public class Clock : IClock
    {
        public DateTimeOffset Now => DateTimeOffset.Now;
    }
}
using System;

namespace Rcm.Common.Temporal;

public class Clock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}

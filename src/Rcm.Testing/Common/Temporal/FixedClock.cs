using System;
using Rcm.Common.Temporal;

namespace Rcm.Testing.Common.Temporal;

public class FixedClock : IClock
{
    public required DateTimeOffset Now { get; set; }
}

using System;

namespace Rcm.Common.Temporal;

public interface IClock
{
    DateTimeOffset Now { get; }
}

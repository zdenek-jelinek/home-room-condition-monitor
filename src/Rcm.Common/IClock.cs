using System;

namespace Rcm.Common;

public interface IClock
{
    DateTimeOffset Now { get; }
}
using System;

namespace Rcm.Common.Temporal;

/// <summary>
/// Provides conversion extensions between temporal types.
/// </summary>
public static class TemporalConversionExtensions
{
    /// <summary>
    /// Converts a <see cref="DateTimeOffset"/> to <see cref="DateOnly"/>, ignoring offset.
    /// </summary>
    /// <param name="time">Time value to convert.</param>
    /// <returns>Converted date value.</returns>
    public static DateOnly ToDateOnly(this DateTimeOffset time)
    {
        return new(time.Year, time.Month, time.Day);
    }
}

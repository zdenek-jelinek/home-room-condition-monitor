using System;

namespace Rcm.Services.Measurements.Collection;

/// <summary>
/// Timings for measurement collection.
/// </summary>
public readonly record struct MeasurementCollectionTimings
{
    /// <summary>
    /// Delay to apply before the first measurement.
    /// </summary>
    public required TimeSpan InitialDelay { get; init; }

    /// <summary>
    /// Delay to apply between measurements.
    /// </summary>
    public required TimeSpan Period { get; init; }
}

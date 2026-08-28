using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Rcm.Common.Temporal;

public readonly struct DateRange
{
    public DateOnly Start { get; }
    public DateOnly End { get; }

    public DateRange(DateOnly start, DateOnly end)
    {
        if (start > end)
        {
            ThrowStartGreaterThanEnd(start, end);
        }

        (Start, End) = (start, end);
    }

    public IEnumerable<DateOnly> EnumerateDates()
    {
        for (var date = Start; date <= End; date = date.AddDays(1))
        {
            yield return date;
        }
    }

    [DoesNotReturn]
    [StackTraceHidden]
    private static void ThrowStartGreaterThanEnd(DateOnly start, DateOnly end)
    {
        throw new ArgumentException($"Start value '{start:o}' is greater than end value '{end:o}'.");
    }
}

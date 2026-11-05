using System;

namespace fake;

/// <summary>Represents an ordered interval used by a synthetic account ledger.</summary>
public class LedgerIntervalRecord
{
    /// <summary>The inclusive beginning of the interval.</summary>
    public DateTimeOffset Start;

    /// <summary>The inclusive end of the interval.</summary>
    public DateTimeOffset End;

    /// <summary>The elapsed time from <c>Start</c> through <c>End</c>.</summary>
    public TimeSpan Duration;

    /// <summary>Whether the end instant is included, always <c>true</c>.</summary>
    public bool EndInclusive;

    internal LedgerIntervalRecord()
    {
    }
}

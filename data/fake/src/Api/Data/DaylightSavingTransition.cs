using System;

namespace fake;

/// <summary>
/// Describes an explicit local-time gap or overlap from a runtime time-zone rule.
/// </summary>
public class DaylightSavingTransition
{
    /// <summary>The canonical bundled IANA time-zone identifier.</summary>
    public string IanaId = string.Empty;

    /// <summary>The calendar year searched for the transition.</summary>
    public int Year;

    /// <summary>Whether the transition is a gap or an overlap.</summary>
    public DaylightSavingTransitionKind Kind;

    /// <summary>The inclusive first invalid or ambiguous local wall-clock value.</summary>
    public DateTime LocalStart;

    /// <summary>The exclusive end of the invalid or ambiguous local wall-clock range.</summary>
    public DateTime LocalEnd;

    /// <summary>The UTC offset in effect immediately before the clock transition.</summary>
    public TimeSpan OffsetBefore;

    /// <summary>The UTC offset in effect immediately after the clock transition.</summary>
    public TimeSpan OffsetAfter;

    /// <summary>The last representable instant before the clock transition.</summary>
    public DateTimeOffset Before;

    /// <summary>The first representable instant after the clock transition.</summary>
    public DateTimeOffset After;

    /// <summary>The earlier instant represented by the middle of an overlap.</summary>
    /// <remarks>For a gap this field equals <see cref="Before"/>.</remarks>
    public DateTimeOffset EarlierOccurrence;

    /// <summary>The later instant represented by the middle of an overlap.</summary>
    /// <remarks>For a gap this field equals <see cref="After"/>.</remarks>
    public DateTimeOffset LaterOccurrence;

    internal DaylightSavingTransition()
    {
        IanaId = string.Empty;
        Year = 1;
        Kind = DaylightSavingTransitionKind.Gap;
        LocalStart = DateTime.MinValue;
        LocalEnd = DateTime.MinValue;
        OffsetBefore = TimeSpan.Zero;
        OffsetAfter = TimeSpan.Zero;
        Before = DateTimeOffset.MinValue;
        After = DateTimeOffset.MinValue;
        EarlierOccurrence = DateTimeOffset.MinValue;
        LaterOccurrence = DateTimeOffset.MinValue;
    }
}

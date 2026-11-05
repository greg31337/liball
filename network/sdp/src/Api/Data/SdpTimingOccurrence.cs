using System;

namespace sdp;

/// <summary>Represents one active timing interval intersected with a caller-provided window.</summary>
public class SdpTimingOccurrence
{
    /// <summary>Contains the inclusive UTC start of the intersected active interval.</summary>
    public DateTimeOffset StartTime = default(DateTimeOffset);

    /// <summary>Contains the exclusive UTC end of the intersected active interval.</summary>
    public DateTimeOffset StopTime = default(DateTimeOffset);

    /// <summary>Identifies the source timing period by zero-based index.</summary>
    public int TimingIndex = -1;

    /// <summary>Identifies the source repeat field by zero-based index, or <c>-1</c> for a non-repeating period.</summary>
    public int RepeatIndex = -1;

    /// <summary>Identifies the source repeat offset by zero-based index, or <c>-1</c> for a non-repeating period.</summary>
    public int OffsetIndex = -1;
}

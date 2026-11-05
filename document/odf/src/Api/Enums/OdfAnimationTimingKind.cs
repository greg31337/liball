namespace odf;

/// <summary>Identifies a parsed SMIL animation timing specifier.</summary>
public enum OdfAnimationTimingKind
{
    /// <summary>The timing expression is absent or unrecognized.</summary>
    Unknown = 0,
    /// <summary>An absolute or relative clock offset.</summary>
    Offset = 1,
    /// <summary>A begin or end synchronization reference.</summary>
    Syncbase = 2,
    /// <summary>An event reference.</summary>
    Event = 3,
    /// <summary>A repeat-event reference.</summary>
    Repeat = 4,
    /// <summary>An access-key event.</summary>
    AccessKey = 5,
    /// <summary>An absolute wall-clock value.</summary>
    WallClock = 6,
    /// <summary>A media marker reference.</summary>
    MediaMarker = 7,
    /// <summary>An indefinite timing value.</summary>
    Indefinite = 8
}

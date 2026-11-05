namespace sdp;

/// <summary>
/// Identifies the kind of difference found between SDP descriptions.
/// </summary>
public enum SdpDifferenceKind
{
    /// <summary>The difference kind is unknown.</summary>
    Unknown = 0,

    /// <summary>A value exists only in the compared description.</summary>
    Added = 1,

    /// <summary>A value exists only in the original description.</summary>
    Removed = 2,

    /// <summary>A value exists in both descriptions but differs.</summary>
    Changed = 3,

    /// <summary>A value has a different relative position.</summary>
    Reordered = 4
}

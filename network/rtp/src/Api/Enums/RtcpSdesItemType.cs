namespace rtp;

/// <summary>
/// Identifies an RFC 3550 source-description item type.
/// </summary>
public enum RtcpSdesItemType
{
    /// <summary>The item terminates a source-description chunk.</summary>
    End = 0,

    /// <summary>The item contains a canonical endpoint identifier.</summary>
    CanonicalName = 1,

    /// <summary>The item contains a display name.</summary>
    Name = 2,

    /// <summary>The item contains an electronic mail address.</summary>
    Email = 3,

    /// <summary>The item contains a telephone number.</summary>
    Phone = 4,

    /// <summary>The item contains a geographic location.</summary>
    Location = 5,

    /// <summary>The item contains an application or tool name.</summary>
    Tool = 6,

    /// <summary>The item contains a notice about the source.</summary>
    Note = 7,

    /// <summary>The item contains a private extension.</summary>
    Private = 8,

    /// <summary>The item type is preserved as an unrecognized value.</summary>
    Unknown = 255
}

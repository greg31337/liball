namespace sdp;

/// <summary>
/// Identifies the SDP scope in which a field or attribute applies.
/// </summary>
public enum SdpScope
{
    /// <summary>The scope is unknown.</summary>
    Unknown = 0,

    /// <summary>The value applies to the session description.</summary>
    Session = 1,

    /// <summary>The value applies to a media description.</summary>
    Media = 2,

    /// <summary>The value applies to an individual media source.</summary>
    Source = 3,

    /// <summary>The value is registered for both session and media scopes.</summary>
    Both = 4
}

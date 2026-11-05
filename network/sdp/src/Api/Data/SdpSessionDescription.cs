using System.Collections.Generic;

namespace sdp;

/// <summary>
/// Represents a complete mutable SDP session description.
/// </summary>
public class SdpSessionDescription
{
    /// <summary>Indicates whether a protocol-version field was present.</summary>
    public bool HasProtocolVersion = false;

    /// <summary>Contains the SDP protocol version.</summary>
    public int ProtocolVersion = 0;

    /// <summary>Contains the required origin field when present.</summary>
    public SdpOrigin? Origin = null;

    /// <summary>Indicates whether a session-name field was present.</summary>
    public bool HasSessionName = false;

    /// <summary>Contains the session name, including an explicitly empty value.</summary>
    public string SessionName = string.Empty;

    /// <summary>Contains session information when an <c>i=</c> field was present.</summary>
    public string? Information = null;

    /// <summary>Contains the session URI when a <c>u=</c> field was present.</summary>
    public string? Uri = null;

    /// <summary>Contains email contact fields in source order.</summary>
    public List<string> Emails = new List<string>();

    /// <summary>Contains phone contact fields in source order.</summary>
    public List<string> Phones = new List<string>();

    /// <summary>Contains the session-level connection field when present.</summary>
    public SdpConnectionData? Connection = null;

    /// <summary>Contains session-level bandwidth fields in source order.</summary>
    public List<SdpBandwidth> Bandwidths = new List<SdpBandwidth>();

    /// <summary>Contains timing periods and their attached recurrences in source order.</summary>
    public List<SdpTiming> Timings = new List<SdpTiming>();

    /// <summary>Contains time-zone adjustment pairs in source order.</summary>
    public List<SdpTimeZoneAdjustment> TimeZoneAdjustments = new List<SdpTimeZoneAdjustment>();

    /// <summary>Contains the obsolete session-level encryption key when present.</summary>
    public SdpEncryptionKey? EncryptionKey = null;

    /// <summary>Contains session-level attributes in source order.</summary>
    public List<SdpAttribute> Attributes = new List<SdpAttribute>();

    /// <summary>Contains media descriptions in source order.</summary>
    public List<SdpMediaDescription> MediaDescriptions = new List<SdpMediaDescription>();

    /// <summary>Contains original session-level lines in source order when lossless data is retained.</summary>
    public List<SdpRawLine> RawLines = new List<SdpRawLine>();

    /// <summary>Identifies the preferred line ending observed in or selected for this description.</summary>
    public SdpLineEnding LineEnding = SdpLineEnding.Unknown;
}

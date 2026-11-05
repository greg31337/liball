using System.Collections.Generic;

namespace sdp;

/// <summary>
/// Represents an SDP media description and its media-level fields.
/// </summary>
public class SdpMediaDescription
{
    /// <summary>Contains the registered or extension media-type token.</summary>
    public string MediaType = string.Empty;

    /// <summary>Identifies the recognized media kind.</summary>
    public SdpMediaKind MediaKind = SdpMediaKind.Unknown;

    /// <summary>Contains the transport port, including zero for a rejected media description.</summary>
    public int Port = 0;

    /// <summary>Indicates whether an explicit port count was present.</summary>
    public bool HasPortCount = false;

    /// <summary>Contains the port count, or one when no explicit count was present.</summary>
    public int PortCount = 1;

    /// <summary>Contains the registered or extension transport-protocol token.</summary>
    public string Protocol = string.Empty;

    /// <summary>Contains the ordered media format tokens.</summary>
    public List<string> Formats = new List<string>();

    /// <summary>Contains media information when an <c>i=</c> field was present.</summary>
    public string? Information = null;

    /// <summary>Contains media-level connection fields in source order.</summary>
    public List<SdpConnectionData> Connections = new List<SdpConnectionData>();

    /// <summary>Contains media-level bandwidth fields in source order.</summary>
    public List<SdpBandwidth> Bandwidths = new List<SdpBandwidth>();

    /// <summary>Contains the obsolete media-level encryption key when present.</summary>
    public SdpEncryptionKey? EncryptionKey = null;

    /// <summary>Contains media-level attributes in source order.</summary>
    public List<SdpAttribute> Attributes = new List<SdpAttribute>();

    /// <summary>Contains original media-level lines in source order when lossless data is retained.</summary>
    public List<SdpRawLine> RawLines = new List<SdpRawLine>();

    /// <summary>Identifies the media field's starting position in the input.</summary>
    public SdpSourceLocation Location = new SdpSourceLocation();
}

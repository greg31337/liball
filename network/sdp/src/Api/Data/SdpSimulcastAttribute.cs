using System.Collections.Generic;

namespace sdp;

/// <summary>Represents typed send and receive simulcast stream lists.</summary>
public class SdpSimulcastAttribute
{
    /// <summary>Contains the originating generic attribute when available.</summary>
    public SdpAttribute? SourceAttribute = null;
    /// <summary>Indicates that the send direction precedes the receive direction when both are present.</summary>
    public bool SendFirst = true;
    /// <summary>Contains send streams in preference order.</summary>
    public List<SdpSimulcastStream> SendStreams = new List<SdpSimulcastStream>();
    /// <summary>Contains receive streams in preference order.</summary>
    public List<SdpSimulcastStream> ReceiveStreams = new List<SdpSimulcastStream>();
    /// <summary>Indicates that <see cref="RawValue"/> contains retained legacy draft syntax.</summary>
    public bool LegacySyntax = false;
    /// <summary>Contains the original value for lossless legacy draft fallback.</summary>
    public string RawValue = string.Empty;
}

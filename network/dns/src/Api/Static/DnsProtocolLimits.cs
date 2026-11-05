namespace dns;

/// <summary>Defines DNS wire-format limits and conservative codec defaults.</summary>
public static class DnsProtocolLimits
{
    /// <summary>The size of a DNS message header in octets.</summary>
    public const int HeaderLength = 12;

    /// <summary>The largest label payload in octets.</summary>
    public const int MaximumLabelLength = 63;

    /// <summary>The largest expanded wire-format name in octets, including the root label.</summary>
    public const int MaximumNameLength = 255;

    /// <summary>The size limit for a classic DNS message sent over UDP.</summary>
    public const int ClassicUdpPayloadSize = 512;

    /// <summary>The conservative EDNS UDP payload size used when no smaller limit is configured.</summary>
    public const int DefaultEdnsUdpPayloadSize = 1232;

    /// <summary>The largest UDP payload size representable by an EDNS OPT record.</summary>
    public const int MaximumEdnsUdpPayloadSize = 65535;

    /// <summary>The size of the DNS-over-TCP message-length prefix in octets.</summary>
    public const int TcpLengthPrefixLength = 2;

    /// <summary>The largest DNS message representable by a DNS-over-TCP length prefix.</summary>
    public const int MaximumTcpMessageLength = 65535;

    /// <summary>The largest section entry count representable by a DNS header.</summary>
    public const int MaximumSectionEntryCount = 65535;

    /// <summary>The size of a DNS compression pointer in octets.</summary>
    public const int CompressionPointerLength = 2;

    /// <summary>The two high bits that identify a DNS compression pointer.</summary>
    public const ushort CompressionPointerTag = 49152;

    /// <summary>The mask for the 14-bit offset carried by a DNS compression pointer.</summary>
    public const ushort CompressionPointerOffsetMask = 16383;

    /// <summary>The largest message offset representable by a DNS compression pointer.</summary>
    public const int MaximumCompressionPointerOffset = 16383;

    /// <summary>The default bound on compression pointers followed while decoding one name.</summary>
    public const int DefaultCompressionPointerHopLimit = 128;
}

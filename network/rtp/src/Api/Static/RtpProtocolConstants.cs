namespace rtp;

/// <summary>
/// Defines RTP fixed-header, field-limit, and sequence-validation constants.
/// </summary>
public static class RtpProtocolConstants
{
    /// <summary>The RTP protocol version.</summary>
    public const byte Version = 2;

    /// <summary>The size of the RTP fixed header in bytes.</summary>
    public const int FixedHeaderSize = 12;

    /// <summary>The size of an SSRC or CSRC identifier in bytes.</summary>
    public const int SourceIdentifierSize = 4;

    /// <summary>The maximum number of CSRC identifiers in an RTP header.</summary>
    public const int MaximumCsrcCount = 15;

    /// <summary>The number of values in the RTP sequence-number space.</summary>
    public const int SequenceNumberModulus = 65536;

    /// <summary>The largest RTP sequence number.</summary>
    public const ushort MaximumSequenceNumber = 65535;

    /// <summary>The RFC 3550 default maximum acceptable forward sequence jump.</summary>
    public const int DefaultMaximumDropout = 3000;

    /// <summary>The RFC 3550 default maximum acceptable sequence misordering.</summary>
    public const int DefaultMaximumMisorder = 100;

    /// <summary>The RFC 3550 default number of sequential packets required for source validation.</summary>
    public const int DefaultMinimumSequential = 2;

    /// <summary>The maximum value of the seven-bit RTP payload-type field.</summary>
    public const byte MaximumPayloadType = 127;

    /// <summary>The size of an RTP header-extension envelope in bytes.</summary>
    public const int HeaderExtensionEnvelopeSize = 4;

    /// <summary>The number of bytes represented by one RTP extension length word.</summary>
    public const int HeaderExtensionWordSize = 4;

    /// <summary>The maximum value of the RTP extension length-in-words field.</summary>
    public const int MaximumHeaderExtensionWordCount = 65535;

    /// <summary>The maximum RTP header-extension data size in bytes.</summary>
    public const int MaximumHeaderExtensionDataSize = 262140;

    /// <summary>The maximum RTP padding count representable by the final byte.</summary>
    public const int MaximumPaddingCount = 255;

    /// <summary>The bit shift of the RTP version field in the first header byte.</summary>
    public const int VersionShift = 6;

    /// <summary>The mask of the RTP version field in the first header byte.</summary>
    public const byte VersionMask = 192;

    /// <summary>The mask of the RTP padding flag in the first header byte.</summary>
    public const byte PaddingMask = 32;

    /// <summary>The mask of the RTP extension flag in the first header byte.</summary>
    public const byte ExtensionMask = 16;

    /// <summary>The mask of the RTP CSRC count in the first header byte.</summary>
    public const byte CsrcCountMask = 15;

    /// <summary>The mask of the RTP marker flag in the second header byte.</summary>
    public const byte MarkerMask = 128;

    /// <summary>The mask of the RTP payload type in the second header byte.</summary>
    public const byte PayloadTypeMask = 127;

    /// <summary>The first RTP payload type forbidden when RTP and RTCP are multiplexed.</summary>
    public const byte MultiplexingForbiddenPayloadTypeMinimum = 64;

    /// <summary>The last RTP payload type forbidden when RTP and RTCP are multiplexed.</summary>
    public const byte MultiplexingForbiddenPayloadTypeMaximum = 95;

    /// <summary>The size of an RFC 4571 stream-frame length prefix in bytes.</summary>
    public const int StreamFrameLengthSize = 2;

    /// <summary>The largest payload representable by an RFC 4571 length prefix.</summary>
    public const int MaximumStreamFramePayloadSize = 65535;
}

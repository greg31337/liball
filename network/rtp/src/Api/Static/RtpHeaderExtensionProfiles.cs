namespace rtp;

/// <summary>
/// Defines RFC 8285 extension profiles, identifiers, and element limits.
/// </summary>
public static class RtpHeaderExtensionProfiles
{
    /// <summary>The profile identifier for the RFC 8285 one-byte format.</summary>
    public const ushort OneByte = 48862;

    /// <summary>The base profile identifier for the RFC 8285 two-byte format.</summary>
    public const ushort TwoByte = 4096;

    /// <summary>The mask used to recognize a two-byte format profile.</summary>
    public const ushort TwoByteProfileMask = 65520;

    /// <summary>The mask of application-defined bits in a two-byte profile.</summary>
    public const ushort TwoByteApplicationBitsMask = 15;

    /// <summary>The padding identifier shared by both RFC 8285 formats.</summary>
    public const int PaddingIdentifier = 0;

    /// <summary>The smallest valid one-byte element identifier.</summary>
    public const int OneByteIdentifierMinimum = 1;

    /// <summary>The largest valid one-byte element identifier.</summary>
    public const int OneByteIdentifierMaximum = 14;

    /// <summary>The one-byte element identifier reserved for termination.</summary>
    public const int OneByteReservedIdentifier = 15;

    /// <summary>The smallest one-byte element data length.</summary>
    public const int OneByteDataLengthMinimum = 1;

    /// <summary>The largest one-byte element data length.</summary>
    public const int OneByteDataLengthMaximum = 16;

    /// <summary>The smallest valid two-byte element identifier.</summary>
    public const int TwoByteIdentifierMinimum = 1;

    /// <summary>The largest valid two-byte element identifier.</summary>
    public const int TwoByteIdentifierMaximum = 255;

    /// <summary>The signaling identifier for the two-byte application bits.</summary>
    public const int TwoByteApplicationBitsIdentifier = 256;

    /// <summary>The smallest two-byte element data length.</summary>
    public const int TwoByteDataLengthMinimum = 0;

    /// <summary>The largest two-byte element data length.</summary>
    public const int TwoByteDataLengthMaximum = 255;

    /// <summary>The size of a one-byte element header.</summary>
    public const int OneByteElementHeaderSize = 1;

    /// <summary>The size of a two-byte element header.</summary>
    public const int TwoByteElementHeaderSize = 2;
}

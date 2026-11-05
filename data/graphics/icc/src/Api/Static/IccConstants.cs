namespace icc;

/// <summary>
/// Defines profile-envelope sizes, masks, versions, and common signatures.
/// </summary>
public static class IccConstants
{
    /// <summary>The profile header size in bytes.</summary>
    public const int HeaderSize = 128;

    /// <summary>The Profile ID size in bytes.</summary>
    public const int ProfileIdSize = 16;

    /// <summary>The encoded tag-count size in bytes.</summary>
    public const int TagCountSize = 4;

    /// <summary>The smallest complete ICC profile size, including an empty tag table.</summary>
    public const int MinimumProfileSize = 132;

    /// <summary>The encoded size of one tag-table entry in bytes.</summary>
    public const int TagTableEntrySize = 12;

    /// <summary>The required tag-payload alignment in bytes.</summary>
    public const int TagPayloadAlignment = 4;

    /// <summary>The largest channel count represented by an ICC.1 colour-space signature.</summary>
    public const int MaximumIcc1ChannelCount = 15;

    /// <summary>The largest channel count represented by an ICC.2 extended signature.</summary>
    public const int MaximumIcc2ChannelCount = 65535;

    /// <summary>The ICC profile file signature <c>acsp</c>.</summary>
    public const uint ProfileFileSignature = 0x61637370;

    /// <summary>The ICC.1 version 2 major version.</summary>
    public const int Version2Major = 2;

    /// <summary>The ICC.1 version 4 major version.</summary>
    public const int Version4Major = 4;

    /// <summary>The ICC.2 version 5 major version.</summary>
    public const int Version5Major = 5;

    /// <summary>The profile-header embedded-profile flag mask.</summary>
    public const uint EmbeddedProfileFlag = 0x00000001;

    /// <summary>The profile-header use-independent-of-data flag mask.</summary>
    public const uint UseIndependentOfDataFlag = 0x00000002;

    /// <summary>The ICC.2 profile-header MCS-subset requirement flag mask.</summary>
    public const uint McsSubsetFlag = 0x00000004;

    /// <summary>The ICC.2 profile-header extended-range colorimetric PCS flag mask.</summary>
    public const uint ExtendedPcsRangeFlag = 0x00000008;

    /// <summary>The device-attributes transparency mask.</summary>
    public const ulong TransparencyAttribute = 0x0000000000000001;

    /// <summary>The device-attributes matte-media mask.</summary>
    public const ulong MatteAttribute = 0x0000000000000002;

    /// <summary>The device-attributes negative-media mask.</summary>
    public const ulong NegativeAttribute = 0x0000000000000004;

    /// <summary>The device-attributes black-and-white-media mask.</summary>
    public const ulong BlackAndWhiteAttribute = 0x0000000000000008;

    /// <summary>The Macintosh platform signature <c>APPL</c>.</summary>
    public const uint MacintoshPlatform = 0x4150504c;

    /// <summary>The Microsoft platform signature <c>MSFT</c>.</summary>
    public const uint MicrosoftPlatform = 0x4d534654;

    /// <summary>The Silicon Graphics platform signature <c>SGI </c>.</summary>
    public const uint SiliconGraphicsPlatform = 0x53474920;

    /// <summary>The Sun platform signature <c>SUNW</c>.</summary>
    public const uint SunPlatform = 0x53554e57;

    /// <summary>The Taligent platform signature <c>TGNT</c>.</summary>
    public const uint TaligentPlatform = 0x54474e54;

    /// <summary>The perceptual reference medium gamut signature <c>prmg</c>.</summary>
    public const uint PerceptualReferenceMediumGamut = 0x70726d67;
}

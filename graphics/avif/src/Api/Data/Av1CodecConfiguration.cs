namespace avif;

/// <summary>Represents one AV1 codec configuration record carried by an <c>av1C</c> box.</summary>
public class Av1CodecConfiguration
{
    /// <summary>The parsing or validation outcome.</summary>
    public AvifResultCode Code;

    /// <summary>The AV1 sequence profile from zero through two.</summary>
    public int Profile;

    /// <summary>The first AV1 sequence level index from zero through 23, or 31.</summary>
    public int Level;

    /// <summary>The first AV1 sequence tier, zero for Main or one for High.</summary>
    public int Tier;

    /// <summary>The coded bit depth: eight, 10, or 12.</summary>
    public int BitDepth;

    /// <summary>Whether the coded sequence is monochrome.</summary>
    public bool Monochrome;

    /// <summary>Whether chroma is horizontally subsampled.</summary>
    public bool ChromaSubsamplingX;

    /// <summary>Whether chroma is vertically subsampled.</summary>
    public bool ChromaSubsamplingY;

    /// <summary>The chroma sample position, applicable to 4:2:0 sequences.</summary>
    public AvifChromaSamplePosition ChromaSamplePosition;

    /// <summary>Whether an initial presentation delay is present.</summary>
    public bool InitialPresentationDelayPresent;

    /// <summary>The initial presentation delay minus one, from zero through 15.</summary>
    public int InitialPresentationDelayMinusOne;

    /// <summary>The optional configuration OBUs following the four-byte record header.</summary>
    public byte[] ConfigObus;

    /// <summary>Creates a valid Main-profile, level-2.0, eight-bit 4:2:0 configuration without OBUs.</summary>
    public Av1CodecConfiguration()
    {
        Code = AvifResultCode.Success;
        Profile = 0;
        Level = 0;
        Tier = 0;
        BitDepth = 8;
        Monochrome = false;
        ChromaSubsamplingX = true;
        ChromaSubsamplingY = true;
        ChromaSamplePosition = AvifChromaSamplePosition.Unknown;
        InitialPresentationDelayPresent = false;
        InitialPresentationDelayMinusOne = 0;
        ConfigObus = System.Array.Empty<byte>();
    }
}

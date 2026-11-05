namespace avif;

/// <summary>Represents AV1 RFC 6381 codec parameters and matching sequence-header signaling.</summary>
public class Av1CodecParameters
{
    /// <summary>The parsing, validation, or reconciliation outcome.</summary>
    public AvifResultCode Code;

    /// <summary>The AV1 sample-entry four-character code, normally <c>av01</c>.</summary>
    public string SampleEntry;

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

    /// <summary>The H.273 color-primary value from zero through 99.</summary>
    public int ColorPrimaries;

    /// <summary>The H.273 transfer-characteristics value from zero through 99.</summary>
    public int TransferCharacteristics;

    /// <summary>The H.273 matrix-coefficients value from zero through 99.</summary>
    public int MatrixCoefficients;

    /// <summary>The encoded integer sample range.</summary>
    public AvifRange Range;

    /// <summary>Whether the mutually inclusive optional RFC 6381 fields are explicitly serialized.</summary>
    public bool HasOptionalFields;

    /// <summary>Creates the abbreviated RFC 6381 defaults for Main-profile eight-bit 4:2:0 AV1.</summary>
    public Av1CodecParameters()
    {
        Code = AvifResultCode.Success;
        SampleEntry = "av01";
        Profile = 0;
        Level = 0;
        Tier = 0;
        BitDepth = 8;
        Monochrome = false;
        ChromaSubsamplingX = true;
        ChromaSubsamplingY = true;
        ChromaSamplePosition = AvifChromaSamplePosition.Unknown;
        ColorPrimaries = 1;
        TransferCharacteristics = 1;
        MatrixCoefficients = 1;
        Range = AvifRange.Limited;
        HasOptionalFields = false;
    }
}

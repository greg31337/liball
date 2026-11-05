namespace raw;

/// <summary>Contains profile-qualified Nikon NEF sensor, calibration, lens, and shooting metadata.</summary>
public class RawNikonNefMetadata
{
    /// <summary>The selected reviewed read-profile identifier.</summary>
    public string ProfileId;
    /// <summary>The normalized Exif camera model.</summary>
    public string Model;
    /// <summary>The complete stored sensor width.</summary>
    public int StoredWidth;
    /// <summary>The complete stored sensor height.</summary>
    public int StoredHeight;
    /// <summary>The active sensor area.</summary>
    public RawArea? ActiveArea;
    /// <summary>The camera-recommended default crop.</summary>
    public RawArea? DefaultCrop;
    /// <summary>Masked or optical-black sensor areas.</summary>
    public RawArea[] MaskedAreas;
    /// <summary>The profile-qualified color filter array.</summary>
    public RawCfaPattern? Cfa;
    /// <summary>The horizontal black-level repeat size.</summary>
    public int BlackLevelRepeatWidth;
    /// <summary>The vertical black-level repeat size.</summary>
    public int BlackLevelRepeatHeight;
    /// <summary>Exact black levels in TIFF repeat order.</summary>
    public double[] BlackLevels;
    /// <summary>Exact sensor white levels.</summary>
    public double[] WhiteLevels;
    /// <summary>The Nikon maker-note data version.</summary>
    public string? MakerNoteVersion;
    /// <summary>The Nikon color-balance block version.</summary>
    public string ColorBalanceVersion;
    /// <summary>Whether the color-balance block is encrypted in the source.</summary>
    public bool ColorBalanceEncrypted;
    /// <summary>Whether the color-balance block was decoded using reviewed rules.</summary>
    public bool ColorBalanceDecoded;
    /// <summary>The exact decoded color-balance levels in <see cref="ColorBalanceChannelOrder"/> order.</summary>
    public double[] AsShotWhiteBalance;
    /// <summary>The channel letters corresponding to <see cref="AsShotWhiteBalance"/>.</summary>
    public string ColorBalanceChannelOrder;
    /// <summary>The TIFF compression identifier from the raw IFD.</summary>
    public int TiffCompressionCode;
    /// <summary>The Nikon maker-note NEFCompression value, or <c>-1</c>.</summary>
    public int NefCompressionCode;
    /// <summary>The resolved raw compression family.</summary>
    public RawCompression Compression;
    /// <summary>The exact TIFF linearization or tone-curve samples.</summary>
    public ushort[] LinearizationTable;
    /// <summary>The Nikon maker-note tag containing compression parameters, or <c>-1</c>.</summary>
    public int CompressionInfoTag;
    /// <summary>The exact Nikon compression-parameter bytes.</summary>
    public byte[] CompressionInfo;
    /// <summary>The byte order of the Nikon compression parameters.</summary>
    public RawByteOrder CompressionInfoByteOrder;
    /// <summary>The Nikon lens-type flags, or <c>-1</c>.</summary>
    public int LensType;
    /// <summary>The exact minimum/maximum focal length and aperture rationals.</summary>
    public RawRational[] LensSpecification;
    /// <summary>The version prefix of Nikon LensData, or an empty string.</summary>
    public string LensDataVersion;
    /// <summary>The exact Nikon LensData bytes.</summary>
    public byte[] LensData;
    /// <summary>The Nikon quality text.</summary>
    public string Quality;
    /// <summary>The Nikon white-balance setting text.</summary>
    public string WhiteBalanceName;
    /// <summary>The Nikon focus-mode text.</summary>
    public string FocusMode;
    /// <summary>The Nikon noise-reduction setting text.</summary>
    public string NoiseReduction;
    /// <summary>The exact Nikon ISO words.</summary>
    public int[] IsoValues;
    /// <summary>The Nikon shooting-mode flags, or <c>-1</c>.</summary>
    public int ShootingMode;
    /// <summary>The Nikon shutter count, or <c>-1</c>.</summary>
    public long ShutterCount;
    /// <summary>Diagnostics produced during profile selection and extraction.</summary>
    public RawDiagnostic[] Diagnostics;

    /// <summary>Creates empty Nikon NEF metadata.</summary>
    public RawNikonNefMetadata()
    {
        ProfileId = string.Empty;
        Model = string.Empty;
        ActiveArea = null;
        DefaultCrop = null;
        MaskedAreas = System.Array.Empty<RawArea>();
        Cfa = null;
        BlackLevelRepeatWidth = 1;
        BlackLevelRepeatHeight = 1;
        BlackLevels = System.Array.Empty<double>();
        WhiteLevels = System.Array.Empty<double>();
        MakerNoteVersion = string.Empty;
        ColorBalanceVersion = string.Empty;
        AsShotWhiteBalance = System.Array.Empty<double>();
        ColorBalanceChannelOrder = string.Empty;
        NefCompressionCode = -1;
        Compression = RawCompression.Unknown;
        LinearizationTable = System.Array.Empty<ushort>();
        CompressionInfoTag = -1;
        CompressionInfo = System.Array.Empty<byte>();
        CompressionInfoByteOrder = RawByteOrder.LittleEndian;
        LensType = -1;
        LensSpecification = System.Array.Empty<RawRational>();
        LensDataVersion = string.Empty;
        LensData = System.Array.Empty<byte>();
        Quality = string.Empty;
        WhiteBalanceName = string.Empty;
        FocusMode = string.Empty;
        NoiseReduction = string.Empty;
        IsoValues = System.Array.Empty<int>();
        ShootingMode = -1;
        ShutterCount = -1;
        Diagnostics = System.Array.Empty<RawDiagnostic>();
    }
}

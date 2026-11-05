namespace raw;

/// <summary>Contains profile-qualified Canon CR2 sensor and calibration metadata.</summary>
public class RawCanonCr2Metadata
{
    /// <summary>The selected reviewed read-profile identifier.</summary>
    public string ProfileId;
    /// <summary>The normalized Exif camera model.</summary>
    public string Model;
    /// <summary>The Canon numeric model identifier.</summary>
    public string ModelId;
    /// <summary>The Canon CameraSettings quality value, or <c>-1</c>.</summary>
    public int Quality;
    /// <summary>The Canon CameraSettings sRAW quality value, or <c>-1</c>.</summary>
    public int SRawQuality;
    /// <summary>The complete stored sensor width.</summary>
    public int SensorWidth;
    /// <summary>The complete stored sensor height.</summary>
    public int SensorHeight;
    /// <summary>The inclusive active left border.</summary>
    public int ActiveLeft;
    /// <summary>The inclusive active top border.</summary>
    public int ActiveTop;
    /// <summary>The inclusive active right border.</summary>
    public int ActiveRight;
    /// <summary>The inclusive active bottom border.</summary>
    public int ActiveBottom;
    /// <summary>The derived active sensor area.</summary>
    public RawArea? ActiveArea;
    /// <summary>Profile-qualified masked or optical-black areas.</summary>
    public RawArea[] MaskedAreas;
    /// <summary>The profile-qualified color filter array.</summary>
    public RawCfaPattern? Cfa;
    /// <summary>The Canon ColorData layout version.</summary>
    public int ColorDataVersion;
    /// <summary>The exact unsigned ColorData words.</summary>
    public ushort[] ColorData;
    /// <summary>The exact as-shot RGGB white-balance levels.</summary>
    public double[] AsShotWhiteBalance;
    /// <summary>The per-CFA-position black levels.</summary>
    public double[] BlackLevels;
    /// <summary>The normal sensor white level when present.</summary>
    public double[] WhiteLevels;
    /// <summary>The profile color matrix, or an empty array when none is qualified.</summary>
    public double[] ColorMatrix;
    /// <summary>The three exact CR2 slice-tag values.</summary>
    public int[] RawSliceValues;
    /// <summary>The expanded profile-observed slice widths.</summary>
    public int[] SliceWidths;
    /// <summary>The exact raw lossless-JPEG payload offset for an early offset-based layout, or <c>-1</c>.</summary>
    public long RawDataOffset;
    /// <summary>The lossless-JPEG coded frame width.</summary>
    public int LosslessJpegCodedWidth;
    /// <summary>The lossless-JPEG coded frame height.</summary>
    public int LosslessJpegCodedHeight;
    /// <summary>The profile-qualified lossless-JPEG component count.</summary>
    public int LosslessJpegComponentCount;
    /// <summary>Whether the coded frame uses the early half-height/double-width layout.</summary>
    public bool HalfHeightDoubleWidth;
    /// <summary>Whether the selected profile contains Canon subsampled RGB source data rather than a CFA mosaic.</summary>
    public bool IsSubsampled;
    /// <summary>The horizontal Canon sRAW/mRAW sampling factor.</summary>
    public int SubsamplingHorizontal;
    /// <summary>The vertical Canon sRAW/mRAW sampling factor.</summary>
    public int SubsamplingVertical;
    /// <summary>The profile-qualified Canon sRAW reconstruction generation.</summary>
    public int SRawGeneration;
    /// <summary>The profile-qualified signed chroma hue adjustment.</summary>
    public int SRawHue;
    /// <summary>The profile-qualified red, green, and blue reconstruction coefficients after any required inversion.</summary>
    public int[] SRawCoefficients;
    /// <summary>Diagnostics produced during profile selection and extraction.</summary>
    public RawDiagnostic[] Diagnostics;

    /// <summary>Creates empty Canon CR2 metadata.</summary>
    public RawCanonCr2Metadata()
    {
        ProfileId = string.Empty;
        Model = string.Empty;
        ModelId = string.Empty;
        Quality = -1;
        SRawQuality = -1;
        ActiveArea = null;
        MaskedAreas = System.Array.Empty<RawArea>();
        Cfa = null;
        ColorData = System.Array.Empty<ushort>();
        AsShotWhiteBalance = System.Array.Empty<double>();
        BlackLevels = System.Array.Empty<double>();
        WhiteLevels = System.Array.Empty<double>();
        ColorMatrix = System.Array.Empty<double>();
        RawSliceValues = System.Array.Empty<int>();
        SliceWidths = System.Array.Empty<int>();
        RawDataOffset = -1;
        LosslessJpegCodedWidth = 0;
        LosslessJpegCodedHeight = 0;
        LosslessJpegComponentCount = 0;
        HalfHeightDoubleWidth = false;
        IsSubsampled = false;
        SubsamplingHorizontal = 1;
        SubsamplingVertical = 1;
        SRawGeneration = -1;
        SRawHue = 0;
        SRawCoefficients = System.Array.Empty<int>();
        Diagnostics = System.Array.Empty<RawDiagnostic>();
    }
}

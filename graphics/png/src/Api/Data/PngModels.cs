namespace png;

/// <summary>
/// Represents a decoded or newly created PNG image, including pixels, metadata, animation frames, and warnings.
/// </summary>
public class PngImage
{
    /// <summary>
    /// Initializes a new instance of the PngImage class with its documented default values.
    /// </summary>
    public PngImage()
    {
    }

    /// <summary>
    /// Stores header-derived dimensions, encoding details, and animation counts.
    /// </summary>
    public PngImageInfo Info = new PngImageInfo();
    /// <summary>
    /// Stores the decoded default image pixels in the requested output format.
    /// </summary>
    public PngPixelBuffer Pixels = new PngPixelBuffer();
    /// <summary>
    /// Stores decoded APNG playback settings and frames; it is empty for a static PNG.
    /// </summary>
    public PngAnimation Animation = new PngAnimation();
    /// <summary>
    /// Stores parsed and preserved chunks that can be edited or written again.
    /// </summary>
    public PngMetadata Metadata = new PngMetadata();
    /// <summary>
    /// Stores non-fatal issues observed while decoding.
    /// </summary>
    public PngWarning[] Warnings = new PngWarning[0];
    /// <summary>
    /// Stores validation errors associated with the result; successful full decoding leaves this array empty.
    /// </summary>
    public PngError[] Errors = new PngError[0];
}

/// <summary>
/// Describes the dimensions and encoded pixel characteristics reported by a PNG datastream.
/// </summary>
public class PngImageInfo
{
    /// <summary>
    /// Initializes a new instance of the PngImageInfo class with its documented default values.
    /// </summary>
    public PngImageInfo()
    {
    }

    /// <summary>
    /// Stores the image width in pixels.
    /// </summary>
    public int Width;
    /// <summary>
    /// Stores the image height in pixels.
    /// </summary>
    public int Height;
    /// <summary>
    /// Stores the color-channel layout declared by IHDR.
    /// </summary>
    public PngColorType ColorType;
    /// <summary>
    /// Stores the per-sample bit depth declared by IHDR.
    /// </summary>
    public PngBitDepth BitDepth;
    /// <summary>
    /// Stores the compression method declared by IHDR.
    /// </summary>
    public PngCompressionMethod CompressionMethod;
    /// <summary>
    /// Stores the scanline filter method declared by IHDR.
    /// </summary>
    public PngFilterMethod FilterMethod;
    /// <summary>
    /// Stores whether the image uses direct scanlines or Adam7 passes.
    /// </summary>
    public PngInterlaceMethod InterlaceMethod;
    /// <summary>
    /// Stores the in-memory format selected for decoded pixels.
    /// </summary>
    public PngPixelFormat PixelFormat;
    /// <summary>
    /// Indicates whether the datastream contains APNG animation control.
    /// </summary>
    public bool IsAnimated;
    /// <summary>
    /// Stores the declared APNG frame count, or zero for a static PNG.
    /// </summary>
    public int FrameCount;
    /// <summary>
    /// Stores the APNG loop count; zero means repeat indefinitely.
    /// </summary>
    public int PlayCount;
}

/// <summary>
/// Stores one 8-bit red, green, and blue entry in an indexed-color palette.
/// </summary>
public class PngPaletteEntry
{
    /// <summary>
    /// Initializes a new instance of the PngPaletteEntry class with its documented default values.
    /// </summary>
    public PngPaletteEntry()
    {
    }

    /// <summary>
    /// Stores the 8-bit red sample.
    /// </summary>
    public byte Red;
    /// <summary>
    /// Stores the 8-bit green sample.
    /// </summary>
    public byte Green;
    /// <summary>
    /// Stores the 8-bit blue sample.
    /// </summary>
    public byte Blue;
}

/// <summary>
/// Stores the ordered color entries used by an indexed-color image.
/// </summary>
public class PngPalette
{
    /// <summary>
    /// Initializes a new instance of the PngPalette class with its documented default values.
    /// </summary>
    public PngPalette()
    {
    }

    /// <summary>
    /// Stores entries in the same zero-based order used by indexed pixel samples.
    /// </summary>
    public PngPaletteEntry[] Entries = new PngPaletteEntry[0];
}

/// <summary>
/// Stores the approximate display frequency for each entry in the primary PNG palette.
/// </summary>
public class PngPaletteHistogram
{
    /// <summary>
    /// Initializes an empty palette histogram.
    /// </summary>
    public PngPaletteHistogram()
    {
    }

    /// <summary>
    /// Stores frequencies in the same order as the corresponding primary palette entries.
    /// </summary>
    public ushort[] Frequencies = new ushort[0];
}

/// <summary>
/// Stores one color and usage-frequency entry from a suggested PNG palette.
/// </summary>
public class PngSuggestedPaletteEntry
{
    /// <summary>
    /// Initializes a suggested palette entry with zero-valued samples and frequency.
    /// </summary>
    public PngSuggestedPaletteEntry()
    {
    }

    /// <summary>
    /// Stores the red sample at the suggested palette's declared sample depth.
    /// </summary>
    public ushort Red;
    /// <summary>
    /// Stores the green sample at the suggested palette's declared sample depth.
    /// </summary>
    public ushort Green;
    /// <summary>
    /// Stores the blue sample at the suggested palette's declared sample depth.
    /// </summary>
    public ushort Blue;
    /// <summary>
    /// Stores the alpha sample at the suggested palette's declared sample depth.
    /// </summary>
    public ushort Alpha;
    /// <summary>
    /// Stores the approximate usage frequency for this entry.
    /// </summary>
    public ushort Frequency;
}

/// <summary>
/// Stores one named suggested palette from an sPLT chunk.
/// </summary>
public class PngSuggestedPalette
{
    /// <summary>
    /// Initializes an empty suggested palette.
    /// </summary>
    public PngSuggestedPalette()
    {
    }

    /// <summary>
    /// Stores the Latin-1 palette name.
    /// </summary>
    public string Name = "";
    /// <summary>
    /// Stores the sample depth, which is either 8 or 16 bits.
    /// </summary>
    public byte SampleDepth;
    /// <summary>
    /// Stores the suggested color entries in chunk order.
    /// </summary>
    public PngSuggestedPaletteEntry[] Entries = new PngSuggestedPaletteEntry[0];
}

/// <summary>
/// Describes the pixel density recorded by a PNG physical-pixel-dimensions chunk.
/// </summary>
public class PngPhysicalPixels
{
    /// <summary>
    /// Initializes a new instance of the PngPhysicalPixels class with its documented default values.
    /// </summary>
    public PngPhysicalPixels()
    {
    }

    /// <summary>
    /// Stores the horizontal pixel density.
    /// </summary>
    public uint PixelsPerUnitX;
    /// <summary>
    /// Stores the vertical pixel density.
    /// </summary>
    public uint PixelsPerUnitY;
    /// <summary>
    /// Stores zero when the unit is unknown or one when the unit is a metre.
    /// </summary>
    public byte UnitSpecifier;
}

/// <summary>
/// Stores per-palette-entry alpha samples from indexed-color transparency metadata.
/// </summary>
public class PngIndexedTransparency
{
    /// <summary>
    /// Initializes a new instance of the PngIndexedTransparency class with its documented default values.
    /// </summary>
    public PngIndexedTransparency()
    {
    }

    /// <summary>
    /// Stores 8-bit alpha by palette index; missing trailing entries are implicitly opaque.
    /// </summary>
    public byte[] Alpha = new byte[0];
}

/// <summary>
/// Identifies the grayscale sample that should be treated as fully transparent.
/// </summary>
public class PngGrayscaleTransparency
{
    /// <summary>
    /// Initializes a new instance of the PngGrayscaleTransparency class with its documented default values.
    /// </summary>
    public PngGrayscaleTransparency()
    {
    }

    /// <summary>
    /// Stores the unscaled encoded grayscale sample that is fully transparent.
    /// </summary>
    public ushort Gray;
}

/// <summary>
/// Identifies the truecolor sample that should be treated as fully transparent.
/// </summary>
public class PngTruecolorTransparency
{
    /// <summary>
    /// Initializes a new instance of the PngTruecolorTransparency class with its documented default values.
    /// </summary>
    public PngTruecolorTransparency()
    {
    }

    /// <summary>
    /// Stores the unscaled encoded red sample of the transparent color.
    /// </summary>
    public ushort Red;
    /// <summary>
    /// Stores the unscaled encoded green sample of the transparent color.
    /// </summary>
    public ushort Green;
    /// <summary>
    /// Stores the unscaled encoded blue sample of the transparent color.
    /// </summary>
    public ushort Blue;
}

/// <summary>
/// Describes how many high-order bits are significant in each encoded color channel.
/// </summary>
public class PngSignificantBits
{
    /// <summary>
    /// Initializes a new instance of the PngSignificantBits class with its documented default values.
    /// </summary>
    public PngSignificantBits()
    {
    }

    /// <summary>
    /// Stores the number of significant bits in a grayscale sample.
    /// </summary>
    public byte Gray;
    /// <summary>
    /// Stores the number of significant bits in a red sample.
    /// </summary>
    public byte Red;
    /// <summary>
    /// Stores the number of significant bits in a green sample.
    /// </summary>
    public byte Green;
    /// <summary>
    /// Stores the number of significant bits in a blue sample.
    /// </summary>
    public byte Blue;
    /// <summary>
    /// Stores the number of significant bits in an alpha sample.
    /// </summary>
    public byte Alpha;
}

/// <summary>
/// Describes the suggested background color in palette, grayscale, or truecolor form.
/// </summary>
public class PngBackgroundColor
{
    /// <summary>
    /// Initializes a new instance of the PngBackgroundColor class with its documented default values.
    /// </summary>
    public PngBackgroundColor()
    {
    }

    /// <summary>
    /// Stores the suggested palette index for indexed-color images.
    /// </summary>
    public byte PaletteIndex;
    /// <summary>
    /// Stores the suggested grayscale sample for grayscale images.
    /// </summary>
    public ushort Gray;
    /// <summary>
    /// Stores the suggested red sample for truecolor images.
    /// </summary>
    public ushort Red;
    /// <summary>
    /// Stores the suggested green sample for truecolor images.
    /// </summary>
    public ushort Green;
    /// <summary>
    /// Stores the suggested blue sample for truecolor images.
    /// </summary>
    public ushort Blue;
}

/// <summary>
/// Stores the white point and primary chromaticities as integers scaled by 100000.
/// </summary>
public class PngChromaticities
{
    /// <summary>
    /// Initializes chromaticities with zero-valued coordinates.
    /// </summary>
    public PngChromaticities()
    {
    }

    /// <summary>
    /// Stores the white-point x coordinate multiplied by 100000.
    /// </summary>
    public uint WhitePointX;
    /// <summary>
    /// Stores the white-point y coordinate multiplied by 100000.
    /// </summary>
    public uint WhitePointY;
    /// <summary>
    /// Stores the red-primary x coordinate multiplied by 100000.
    /// </summary>
    public uint RedX;
    /// <summary>
    /// Stores the red-primary y coordinate multiplied by 100000.
    /// </summary>
    public uint RedY;
    /// <summary>
    /// Stores the green-primary x coordinate multiplied by 100000.
    /// </summary>
    public uint GreenX;
    /// <summary>
    /// Stores the green-primary y coordinate multiplied by 100000.
    /// </summary>
    public uint GreenY;
    /// <summary>
    /// Stores the blue-primary x coordinate multiplied by 100000.
    /// </summary>
    public uint BlueX;
    /// <summary>
    /// Stores the blue-primary y coordinate multiplied by 100000.
    /// </summary>
    public uint BlueY;
}

/// <summary>
/// Stores PNG image gamma as an integer scaled by 100000.
/// </summary>
public class PngGamma
{
    /// <summary>
    /// Initializes a new instance of the PngGamma class with its documented default values.
    /// </summary>
    public PngGamma()
    {
    }

    /// <summary>
    /// Stores image gamma multiplied by 100000; zero is not a valid PNG gamma value.
    /// </summary>
    public uint Value;
}

/// <summary>
/// Stores the rendering intent declared by standard RGB color metadata.
/// </summary>
public class PngStandardRgb
{
    /// <summary>
    /// Initializes a new instance of the PngStandardRgb class with its documented default values.
    /// </summary>
    public PngStandardRgb()
    {
    }

    /// <summary>
    /// Stores the PNG rendering-intent code from 0 through 3.
    /// </summary>
    public byte RenderingIntent;
}

/// <summary>
/// Stores an ICC profile name together with its decompressed and compressed payloads.
/// </summary>
public class PngIccProfile
{
    /// <summary>
    /// Initializes a new instance of the PngIccProfile class with its documented default values.
    /// </summary>
    public PngIccProfile()
    {
    }

    /// <summary>
    /// Stores the Latin-1 profile name written before the compressed payload.
    /// </summary>
    public string Name = "";
    /// <summary>
    /// Stores the profile compression method; PNG currently defines only zero.
    /// </summary>
    public byte CompressionMethod;
    /// <summary>
    /// Stores the decompressed ICC profile bytes.
    /// </summary>
    public byte[] Profile = new byte[0];
    /// <summary>
    /// Stores the original compressed payload when it was preserved while reading.
    /// </summary>
    public byte[] CompressedProfile = new byte[0];
}

/// <summary>
/// Stores the four coding-independent color and video signal code points defined by PNG.
/// </summary>
public class PngCodingIndependentCodePoints
{
    /// <summary>
    /// Initializes a new instance of the PngCodingIndependentCodePoints class with its documented default values.
    /// </summary>
    public PngCodingIndependentCodePoints()
    {
    }

    /// <summary>
    /// Stores the standardized color-primary code point.
    /// </summary>
    public byte ColorPrimaries;
    /// <summary>
    /// Stores the standardized transfer-characteristic code point.
    /// </summary>
    public byte TransferCharacteristics;
    /// <summary>
    /// Stores the standardized matrix-coefficient code point.
    /// </summary>
    public byte MatrixCoefficients;
    /// <summary>
    /// Stores zero for narrow range or one for full range.
    /// </summary>
    public byte VideoFullRangeFlag;
}

/// <summary>
/// Stores mastering-display primaries, white point, and luminance from an mDCV chunk.
/// </summary>
public class PngMasteringDisplayColorVolume
{
    /// <summary>
    /// Initializes mastering-display metadata with zero-valued fields.
    /// </summary>
    public PngMasteringDisplayColorVolume()
    {
    }

    /// <summary>
    /// Stores the red-primary x coordinate in the mDCV encoded units.
    /// </summary>
    public ushort RedX;
    /// <summary>
    /// Stores the red-primary y coordinate in the mDCV encoded units.
    /// </summary>
    public ushort RedY;
    /// <summary>
    /// Stores the green-primary x coordinate in the mDCV encoded units.
    /// </summary>
    public ushort GreenX;
    /// <summary>
    /// Stores the green-primary y coordinate in the mDCV encoded units.
    /// </summary>
    public ushort GreenY;
    /// <summary>
    /// Stores the blue-primary x coordinate in the mDCV encoded units.
    /// </summary>
    public ushort BlueX;
    /// <summary>
    /// Stores the blue-primary y coordinate in the mDCV encoded units.
    /// </summary>
    public ushort BlueY;
    /// <summary>
    /// Stores the mastering white-point x coordinate in the mDCV encoded units.
    /// </summary>
    public ushort WhitePointX;
    /// <summary>
    /// Stores the mastering white-point y coordinate in the mDCV encoded units.
    /// </summary>
    public ushort WhitePointY;
    /// <summary>
    /// Stores the maximum mastering-display luminance in the mDCV encoded units.
    /// </summary>
    public uint MaximumLuminance;
    /// <summary>
    /// Stores the minimum mastering-display luminance in the mDCV encoded units.
    /// </summary>
    public uint MinimumLuminance;
}

/// <summary>
/// Stores content light-level information from a cLLI chunk.
/// </summary>
public class PngContentLightLevelInformation
{
    /// <summary>
    /// Initializes content light-level metadata with zero-valued fields.
    /// </summary>
    public PngContentLightLevelInformation()
    {
    }

    /// <summary>
    /// Stores the maximum content light level in the cLLI encoded units.
    /// </summary>
    public uint MaximumContentLightLevel;
    /// <summary>
    /// Stores the maximum frame-average light level in the cLLI encoded units.
    /// </summary>
    public uint MaximumFrameAverageLightLevel;
}

/// <summary>
/// Stores one uncompressed Latin-1 PNG text entry.
/// </summary>
public class PngText
{
    /// <summary>
    /// Initializes a new instance of the PngText class with its documented default values.
    /// </summary>
    public PngText()
    {
    }

    /// <summary>
    /// Stores the 1-to-79-byte Latin-1 keyword used to identify the text entry.
    /// </summary>
    public string Keyword = "";
    /// <summary>
    /// Stores the uncompressed Latin-1 text value.
    /// </summary>
    public string Text = "";
}

/// <summary>
/// Stores one zlib-compressed Latin-1 PNG text entry.
/// </summary>
public class PngCompressedText
{
    /// <summary>
    /// Initializes an empty compressed Latin-1 text entry.
    /// </summary>
    public PngCompressedText()
    {
    }

    /// <summary>
    /// Stores the 1-to-79-byte Latin-1 keyword.
    /// </summary>
    public string Keyword = "";
    /// <summary>
    /// Stores the compression method; PNG currently defines only zero.
    /// </summary>
    public byte CompressionMethod;
    /// <summary>
    /// Stores the decompressed Latin-1 text.
    /// </summary>
    public string Text = "";
    /// <summary>
    /// Stores the original compressed text bytes when available.
    /// </summary>
    public byte[] CompressedText = new byte[0];
}

/// <summary>
/// Stores one international UTF-8 PNG text entry.
/// </summary>
public class PngInternationalText
{
    /// <summary>
    /// Initializes an empty international text entry.
    /// </summary>
    public PngInternationalText()
    {
    }

    /// <summary>
    /// Stores the 1-to-79-byte Latin-1 keyword.
    /// </summary>
    public string Keyword = "";
    /// <summary>
    /// Stores zero for uncompressed text or one for compressed text.
    /// </summary>
    public byte CompressionFlag;
    /// <summary>
    /// Stores the compression method used when compression is enabled.
    /// </summary>
    public byte CompressionMethod;
    /// <summary>
    /// Stores the ASCII language tag, or an empty string when unspecified.
    /// </summary>
    public string LanguageTag = "";
    /// <summary>
    /// Stores the translated UTF-8 keyword.
    /// </summary>
    public string TranslatedKeyword = "";
    /// <summary>
    /// Stores the decoded UTF-8 text.
    /// </summary>
    public string Text = "";
    /// <summary>
    /// Stores the original compressed text bytes when compression is enabled and the payload is available.
    /// </summary>
    public byte[] CompressedText = new byte[0];
}

/// <summary>
/// Stores a lossless, ordered public view of a tEXt, zTXt, or iTXt entry.
/// </summary>
public class PngTextEntry
{
    /// <summary>
    /// Initializes an empty uncompressed Latin-1 text entry that is not associated with an original chunk index.
    /// </summary>
    public PngTextEntry()
    {
    }

    /// <summary>
    /// Identifies whether the entry came from tEXt, zTXt, or iTXt.
    /// </summary>
    public PngTextKind Kind;
    /// <summary>
    /// Stores the original zero-based chunk index, or minus one for a newly created entry.
    /// </summary>
    public int OriginalChunkIndex = -1;
    /// <summary>
    /// Stores the 1-to-79-byte Latin-1 keyword shared by all PNG text chunk forms.
    /// </summary>
    public string Keyword = "";
    /// <summary>
    /// Stores zero for uncompressed text or one for compressed text.
    /// </summary>
    public byte CompressionFlag;
    /// <summary>
    /// Stores the compression method when compression is enabled.
    /// </summary>
    public byte CompressionMethod;
    /// <summary>
    /// Stores the ASCII language tag for iTXt, or an empty string for other forms or an unspecified language.
    /// </summary>
    public string LanguageTag = "";
    /// <summary>
    /// Stores the translated UTF-8 keyword for iTXt, or an empty string for other forms.
    /// </summary>
    public string TranslatedKeyword = "";
    /// <summary>
    /// Stores decoded Latin-1 or UTF-8 text according to Kind.
    /// </summary>
    public string Text = "";
    /// <summary>
    /// Stores the original compressed text bytes when present so re-encoding need not recompress the payload.
    /// </summary>
    public byte[] CompressedText = new byte[0];
}

/// <summary>
/// Stores an Exif profile payload beginning with its TIFF header.
/// </summary>
public class PngExifProfile
{
    /// <summary>
    /// Initializes an empty Exif profile.
    /// </summary>
    public PngExifProfile()
    {
    }

    /// <summary>
    /// Stores the complete Exif payload without an Exif text signature.
    /// </summary>
    public byte[] Profile = new byte[0];
}

/// <summary>
/// Stores the last-modification time represented by a PNG tIME chunk.
/// </summary>
public class PngTime
{
    /// <summary>
    /// Initializes a time value with zero-valued fields.
    /// </summary>
    public PngTime()
    {
    }

    /// <summary>
    /// Stores the four-digit year.
    /// </summary>
    public ushort Year;
    /// <summary>
    /// Stores the month from 1 through 12.
    /// </summary>
    public byte Month;
    /// <summary>
    /// Stores the day from 1 through 31.
    /// </summary>
    public byte Day;
    /// <summary>
    /// Stores the hour from 0 through 23.
    /// </summary>
    public byte Hour;
    /// <summary>
    /// Stores the minute from 0 through 59.
    /// </summary>
    public byte Minute;
    /// <summary>
    /// Stores the second from 0 through 60 to permit leap seconds.
    /// </summary>
    public byte Second;
}

/// <summary>
/// Records the encoded output and outcome for one compression-level optimization trial.
/// </summary>
public class PngCompressionTrial
{
    /// <summary>
    /// Initializes a new instance of the PngCompressionTrial class with its documented default values.
    /// </summary>
    public PngCompressionTrial()
    {
    }

    /// <summary>
    /// Stores the Deflate level used for this encode.
    /// </summary>
    public int CompressionLevel;
    /// <summary>
    /// Stores the encoded PNG bytes produced by this trial.
    /// </summary>
    public byte[] EncodedBytes = new byte[0];
    /// <summary>
    /// Stores the encoded length, or the bytes produced before bailout.
    /// </summary>
    public long ByteCount;
    /// <summary>
    /// Indicates whether this is the smallest completed trial.
    /// </summary>
    public bool IsBest;
    /// <summary>
    /// Indicates whether encoding stopped after exceeding the current best size.
    /// </summary>
    public bool WasBailedOut;
}

/// <summary>
/// Collects compression-level trials and identifies the smallest result.
/// </summary>
public class PngCompressionTrialResult
{
    /// <summary>
    /// Initializes a new instance of the PngCompressionTrialResult class with its documented default values.
    /// </summary>
    public PngCompressionTrialResult()
    {
    }

    /// <summary>
    /// Stores completed and bailed-out trials in attempted order.
    /// </summary>
    public PngCompressionTrial[] Trials = new PngCompressionTrial[0];
    /// <summary>
    /// Stores the smallest completed trial.
    /// </summary>
    public PngCompressionTrial BestTrial = new PngCompressionTrial();
    /// <summary>
    /// Indicates whether more than the baseline compression level was attempted.
    /// </summary>
    public bool TriedCompressionLevels;
    /// <summary>
    /// Stores how many trials stopped early.
    /// </summary>
    public int BailedOutTrialCount;
}

/// <summary>
/// Records the encoded output and outcome for one row-filter strategy trial.
/// </summary>
public class PngFilterStrategyTrial
{
    /// <summary>
    /// Initializes a new instance of the PngFilterStrategyTrial class with its documented default values.
    /// </summary>
    public PngFilterStrategyTrial()
    {
    }

    /// <summary>
    /// Stores the Deflate compression level used for this filter trial.
    /// </summary>
    public int CompressionLevel;
    /// <summary>
    /// Stores the forced filter, or the baseline filter setting for an adaptive trial.
    /// </summary>
    public PngFilterType FilterType;
    /// <summary>
    /// Stores the row-filter strategy used for this trial.
    /// </summary>
    public PngRowFilterStrategy RowFilterStrategy = PngRowFilterStrategy.Adaptive;
    /// <summary>
    /// Indicates whether this trial forces one filter type for every row.
    /// </summary>
    public bool UseFixedFilter;
    /// <summary>
    /// Stores the encoded PNG bytes produced by this trial.
    /// </summary>
    public byte[] EncodedBytes = new byte[0];
    /// <summary>
    /// Stores the encoded length, or the bytes produced before bailout.
    /// </summary>
    public long ByteCount;
    /// <summary>
    /// Indicates whether this is the smallest completed trial.
    /// </summary>
    public bool IsBest;
    /// <summary>
    /// Indicates whether encoding stopped after exceeding the current best size.
    /// </summary>
    public bool WasBailedOut;
}

/// <summary>
/// Collects filter-strategy trials and identifies the smallest result.
/// </summary>
public class PngFilterStrategyTrialResult
{
    /// <summary>
    /// Initializes a new instance of the PngFilterStrategyTrialResult class with its documented default values.
    /// </summary>
    public PngFilterStrategyTrialResult()
    {
    }

    /// <summary>
    /// Stores completed and bailed-out filter trials in attempted order.
    /// </summary>
    public PngFilterStrategyTrial[] Trials = new PngFilterStrategyTrial[0];
    /// <summary>
    /// Stores the smallest completed filter trial.
    /// </summary>
    public PngFilterStrategyTrial BestTrial = new PngFilterStrategyTrial();
    /// <summary>
    /// Indicates whether alternative filter strategies were attempted.
    /// </summary>
    public bool TriedFilterStrategies;
    /// <summary>
    /// Stores how many filter trials stopped early.
    /// </summary>
    public int BailedOutTrialCount;
}

/// <summary>
/// Describes one choice made by the complete PNG optimization workflow.
/// </summary>
public class PngOptimizationDecision
{
    /// <summary>
    /// Initializes an empty optimization decision.
    /// </summary>
    public PngOptimizationDecision()
    {
    }

    /// <summary>
    /// Stores a stable identifier for the decision.
    /// </summary>
    public string Code = "";
    /// <summary>
    /// Stores a human-readable explanation of the choice.
    /// </summary>
    public string Description = "";
    /// <summary>
    /// Indicates whether the corresponding optimization was permitted or requested.
    /// </summary>
    public bool Enabled;
    /// <summary>
    /// Indicates whether the choice changed the winning output or stopped work early.
    /// </summary>
    public bool Applied;
}

/// <summary>
/// Summarizes the byte savings and winning settings from compression and filter trials.
/// </summary>
public class PngOptimizerReport
{
    /// <summary>
    /// Initializes a new instance of the PngOptimizerReport class with its documented default values.
    /// </summary>
    public PngOptimizerReport()
    {
    }

    /// <summary>
    /// Stores the input PNG length used as the comparison baseline.
    /// </summary>
    public long OriginalByteCount;
    /// <summary>
    /// Stores the smallest completed output length.
    /// </summary>
    public long BestByteCount;
    /// <summary>
    /// Stores OriginalByteCount minus BestByteCount; negative values indicate growth.
    /// </summary>
    public long ByteSavings;
    /// <summary>
    /// Stores the bytes from the smallest completed trial.
    /// </summary>
    public byte[] BestEncodedBytes = new byte[0];
    /// <summary>
    /// Indicates whether at least one completed trial produced output bytes.
    /// </summary>
    public bool FoundBestResult;
    /// <summary>
    /// Indicates whether the winning output came from a compression-level trial.
    /// </summary>
    public bool BestFromCompressionTrial;
    /// <summary>
    /// Indicates whether the winning output came from a filter-strategy trial.
    /// </summary>
    public bool BestFromFilterStrategyTrial;
    /// <summary>
    /// Stores the Deflate level used by the winning output.
    /// </summary>
    public int BestCompressionLevel;
    /// <summary>
    /// Stores the filter type associated with the winning filter trial.
    /// </summary>
    public PngFilterType BestFilterType;
    /// <summary>
    /// Indicates whether the winning filter trial forced one filter on every row.
    /// </summary>
    public bool BestUsesFixedFilter;
    /// <summary>
    /// Stores the number of compression-level trials considered.
    /// </summary>
    public int CompressionTrialCount;
    /// <summary>
    /// Stores the number of filter-strategy trials considered.
    /// </summary>
    public int FilterStrategyTrialCount;
    /// <summary>
    /// Stores the combined number of trials that stopped early.
    /// </summary>
    public int BailedOutTrialCount;
    /// <summary>
    /// Indicates that the input datastream remained smaller than every completed trial.
    /// </summary>
    public bool BestIsOriginal;
    /// <summary>
    /// Stores the row-filter strategy associated with the winning trial.
    /// </summary>
    public PngRowFilterStrategy BestRowFilterStrategy = PngRowFilterStrategy.Adaptive;
    /// <summary>
    /// Stores the source PNG color type.
    /// </summary>
    public PngColorType OriginalColorType;
    /// <summary>
    /// Stores the color type of the winning PNG.
    /// </summary>
    public PngColorType OptimizedColorType;
    /// <summary>
    /// Stores the source PNG bit depth.
    /// </summary>
    public PngBitDepth OriginalBitDepth;
    /// <summary>
    /// Stores the bit depth of the winning PNG.
    /// </summary>
    public PngBitDepth OptimizedBitDepth;
    /// <summary>
    /// Stores the source palette entry count.
    /// </summary>
    public int OriginalPaletteEntryCount;
    /// <summary>
    /// Stores the winning palette entry count.
    /// </summary>
    public int OptimizedPaletteEntryCount;
    /// <summary>
    /// Stores the source ancillary metadata payload byte count.
    /// </summary>
    public long OriginalMetadataBytes;
    /// <summary>
    /// Stores the winning ancillary metadata payload byte count.
    /// </summary>
    public long OptimizedMetadataBytes;
    /// <summary>
    /// Stores every decision made by the complete workflow.
    /// </summary>
    public PngOptimizationDecision[] Decisions = new PngOptimizationDecision[0];
    /// <summary>
    /// Stores the compression trials performed by the complete workflow.
    /// </summary>
    public PngCompressionTrialResult CompressionResult = new PngCompressionTrialResult();
    /// <summary>
    /// Stores the filter-strategy trials performed by the complete workflow.
    /// </summary>
    public PngFilterStrategyTrialResult FilterStrategyResult = new PngFilterStrategyTrialResult();
}

/// <summary>
/// Stores pixels in a declared format with explicit dimensions, stride, and backing bytes.
/// </summary>
public class PngPixelBuffer
{
    /// <summary>
    /// Initializes a new instance of the PngPixelBuffer class with its documented default values.
    /// </summary>
    public PngPixelBuffer()
    {
    }

    /// <summary>
    /// Stores the channel order, sample depth, and packed-index interpretation of Data.
    /// </summary>
    public PngPixelFormat Format;
    /// <summary>
    /// Stores the number of pixels in each row.
    /// </summary>
    public int Width;
    /// <summary>
    /// Stores the number of rows.
    /// </summary>
    public int Height;
    /// <summary>
    /// Stores the byte distance between the start of adjacent rows, including any padding.
    /// </summary>
    public int Stride;
    /// <summary>
    /// Stores at least Stride multiplied by Height bytes; 16-bit samples use big-endian byte order.
    /// </summary>
    public byte[] Data = new byte[0];
}

/// <summary>
/// Stores the frame and playback counts from an APNG animation-control chunk.
/// </summary>
public class PngAnimationControl
{
    /// <summary>
    /// Initializes animation control with zero-valued counts.
    /// </summary>
    public PngAnimationControl()
    {
    }

    /// <summary>
    /// Stores the declared number of animation frames.
    /// </summary>
    public uint FrameCount;
    /// <summary>
    /// Stores the number of animation plays; zero means repeat indefinitely.
    /// </summary>
    public uint PlayCount;
}

/// <summary>
/// Stores the sequence, rectangle, timing, disposal, and blending fields from an APNG frame-control chunk.
/// </summary>
public class PngFrameControl
{
    /// <summary>
    /// Initializes frame control with zero-valued fields and default operations.
    /// </summary>
    public PngFrameControl()
    {
    }

    /// <summary>
    /// Stores the APNG sequence number shared with frame-data chunks.
    /// </summary>
    public uint SequenceNumber;
    /// <summary>
    /// Stores the frame rectangle width.
    /// </summary>
    public uint Width;
    /// <summary>
    /// Stores the frame rectangle height.
    /// </summary>
    public uint Height;
    /// <summary>
    /// Stores the horizontal frame offset from the canvas origin.
    /// </summary>
    public uint XOffset;
    /// <summary>
    /// Stores the vertical frame offset from the canvas origin.
    /// </summary>
    public uint YOffset;
    /// <summary>
    /// Stores the frame delay numerator.
    /// </summary>
    public ushort DelayNumerator;
    /// <summary>
    /// Stores the encoded frame delay denominator; zero is interpreted as 100.
    /// </summary>
    public ushort DelayDenominator;
    /// <summary>
    /// Stores how the output canvas is changed after displaying the frame.
    /// </summary>
    public PngApngDisposeOperation DisposeOperation;
    /// <summary>
    /// Stores how the frame rectangle is combined with the output canvas.
    /// </summary>
    public PngApngBlendOperation BlendOperation;
}

/// <summary>
/// Stores one APNG frame-data sequence number and its frame-local compressed stream fragment.
/// </summary>
public class PngFrameData
{
    /// <summary>
    /// Initializes an empty frame-data fragment.
    /// </summary>
    public PngFrameData()
    {
    }

    /// <summary>
    /// Stores the APNG sequence number shared with frame-control chunks.
    /// </summary>
    public uint SequenceNumber;
    /// <summary>
    /// Stores compressed frame data without the four-byte sequence-number prefix.
    /// </summary>
    public byte[] Data = new byte[0];
}

/// <summary>
/// Represents one APNG frame rectangle, its timing and composition operations, and its pixels.
/// </summary>
public class PngFrame
{
    /// <summary>
    /// Initializes a new instance of the PngFrame class with its documented default values.
    /// </summary>
    public PngFrame()
    {
    }

    /// <summary>
    /// Stores the frame rectangle width in pixels.
    /// </summary>
    public int Width;
    /// <summary>
    /// Stores the frame rectangle height in pixels.
    /// </summary>
    public int Height;
    /// <summary>
    /// Stores the frame rectangle's horizontal offset from the canvas origin.
    /// </summary>
    public int XOffset;
    /// <summary>
    /// Stores the frame rectangle's vertical offset from the canvas origin.
    /// </summary>
    public int YOffset;
    /// <summary>
    /// Stores the frame delay numerator.
    /// </summary>
    public int DelayNumerator;
    /// <summary>
    /// Stores the frame delay denominator; APNG interprets zero as 100.
    /// </summary>
    public int DelayDenominator;
    /// <summary>
    /// Stores how frame pixels are combined with the existing canvas.
    /// </summary>
    public PngApngBlendOperation BlendOperation;
    /// <summary>
    /// Stores how the canvas is changed after this frame is displayed.
    /// </summary>
    public PngApngDisposeOperation DisposeOperation;
    /// <summary>
    /// Stores pixels for the frame rectangle, not necessarily the full canvas.
    /// </summary>
    public PngPixelBuffer Pixels = new PngPixelBuffer();
    /// <summary>
    /// Indicates whether this frame uses the PNG default image data.
    /// </summary>
    public bool UsesDefaultImage;
}

/// <summary>
/// Stores APNG playback settings and frames in display order.
/// </summary>
public class PngAnimation
{
    /// <summary>
    /// Initializes a new instance of the PngAnimation class with its documented default values.
    /// </summary>
    public PngAnimation()
    {
    }

    /// <summary>
    /// Stores the declared number of frames.
    /// </summary>
    public int FrameCount;
    /// <summary>
    /// Stores the loop count; zero means repeat indefinitely.
    /// </summary>
    public int PlayCount;
    /// <summary>
    /// Stores decoded frames in APNG sequence order.
    /// </summary>
    public PngFrame[] Frames = new PngFrame[0];
    /// <summary>
    /// Stores each displayed full canvas after blending its corresponding frame, using RGBA8 or RGBA16 pixels.
    /// </summary>
    public PngPixelBuffer[] ComposedFrames = new PngPixelBuffer[0];
}

/// <summary>
/// Describes the effective PNG color metadata selected by the defined cICP, iCCP, sRGB, and cHRM/gAMA precedence rules.
/// </summary>
public class PngColorMetadata
{
    /// <summary>
    /// Initializes an empty color-metadata result.
    /// </summary>
    public PngColorMetadata()
    {
    }

    /// <summary>
    /// Indicates whether any supported color metadata was selected.
    /// </summary>
    public bool HasColorMetadata;
    /// <summary>
    /// Stores the selected source chunk type, or <c>cHRM/gAMA</c> when both lower-precedence chunks contribute.
    /// </summary>
    public string SourceChunkType = "";
    /// <summary>
    /// Stores the selected precedence rank, where zero means absent and smaller positive values have higher precedence.
    /// </summary>
    public int Precedence;
    /// <summary>
    /// Indicates whether cICP supplied the selected color metadata.
    /// </summary>
    public bool HasCodingIndependentCodePoints;
    /// <summary>
    /// Stores selected cICP code points when present.
    /// </summary>
    public PngCodingIndependentCodePoints CodingIndependentCodePoints = new PngCodingIndependentCodePoints();
    /// <summary>
    /// Indicates whether iCCP supplied the selected color metadata.
    /// </summary>
    public bool HasIccProfile;
    /// <summary>
    /// Stores the selected decompressed ICC profile when present.
    /// </summary>
    public PngIccProfile IccProfile = new PngIccProfile();
    /// <summary>
    /// Indicates whether sRGB supplied the selected color metadata.
    /// </summary>
    public bool HasStandardRgb;
    /// <summary>
    /// Stores the selected sRGB rendering intent when present.
    /// </summary>
    public PngStandardRgb StandardRgb = new PngStandardRgb();
    /// <summary>
    /// Indicates whether cHRM contributes to the selected color metadata.
    /// </summary>
    public bool HasChromaticities;
    /// <summary>
    /// Stores selected cHRM chromaticities when present.
    /// </summary>
    public PngChromaticities Chromaticities = new PngChromaticities();
    /// <summary>
    /// Indicates whether gAMA contributes to the selected color metadata.
    /// </summary>
    public bool HasGamma;
    /// <summary>
    /// Stores selected gAMA image gamma when present.
    /// </summary>
    public PngGamma Gamma = new PngGamma();
}

/// <summary>
/// Stores raw PNG chunks, preserved unknown chunks, palette data, and metadata size totals.
/// </summary>
public class PngMetadata
{
    /// <summary>
    /// Initializes a new instance of the PngMetadata class with its documented default values.
    /// </summary>
    public PngMetadata()
    {
    }

    /// <summary>
    /// Stores preserved chunks in datastream order, excluding signature framing.
    /// </summary>
    public PngChunk[] Chunks = new PngChunk[0];
    /// <summary>
    /// Stores the subset of chunks whose types are not recognized by the library.
    /// </summary>
    public PngChunk[] UnknownChunks = new PngChunk[0];
    /// <summary>
    /// Stores the parsed primary palette, or an empty palette when no PLTE chunk exists.
    /// </summary>
    public PngPalette Palette = new PngPalette();
    /// <summary>
    /// Stores indexed-color transparency metadata when a tRNS chunk applies to a palette image.
    /// </summary>
    public PngIndexedTransparency? IndexedTransparency;
    /// <summary>
    /// Stores grayscale transparency metadata when a tRNS chunk applies to a grayscale image.
    /// </summary>
    public PngGrayscaleTransparency? GrayscaleTransparency;
    /// <summary>
    /// Stores truecolor transparency metadata when a tRNS chunk applies to a truecolor image.
    /// </summary>
    public PngTruecolorTransparency? TruecolorTransparency;
    /// <summary>
    /// Stores cHRM chromaticities when present.
    /// </summary>
    public PngChromaticities? Chromaticities;
    /// <summary>
    /// Stores gAMA image gamma when present.
    /// </summary>
    public PngGamma? Gamma;
    /// <summary>
    /// Stores the decompressed iCCP profile and its associated fields when present.
    /// </summary>
    public PngIccProfile? IccProfile;
    /// <summary>
    /// Stores sBIT significant-bit metadata when present.
    /// </summary>
    public PngSignificantBits? SignificantBits;
    /// <summary>
    /// Stores sRGB rendering-intent metadata when present.
    /// </summary>
    public PngStandardRgb? StandardRgb;
    /// <summary>
    /// Stores cICP coding-independent code points when present.
    /// </summary>
    public PngCodingIndependentCodePoints? CodingIndependentCodePoints;
    /// <summary>
    /// Stores the effective color metadata selected according to PNG color-chunk precedence.
    /// </summary>
    public PngColorMetadata ColorMetadata = new PngColorMetadata();
    /// <summary>
    /// Stores mDCV mastering-display metadata when present.
    /// </summary>
    public PngMasteringDisplayColorVolume? MasteringDisplayColorVolume;
    /// <summary>
    /// Stores cLLI content light-level metadata when present.
    /// </summary>
    public PngContentLightLevelInformation? ContentLightLevelInformation;
    /// <summary>
    /// Stores all standard PNG text entries in original datastream order without discarding family-specific fields.
    /// </summary>
    public PngTextEntry[] TextEntries = new PngTextEntry[0];
    /// <summary>
    /// Stores uncompressed Latin-1 tEXt entries in their family-specific order.
    /// </summary>
    public PngText[] Latin1TextEntries = new PngText[0];
    /// <summary>
    /// Stores compressed Latin-1 zTXt entries in their family-specific order.
    /// </summary>
    public PngCompressedText[] CompressedTextEntries = new PngCompressedText[0];
    /// <summary>
    /// Stores international UTF-8 iTXt entries in their family-specific order.
    /// </summary>
    public PngInternationalText[] InternationalTextEntries = new PngInternationalText[0];
    /// <summary>
    /// Stores bKGD suggested background metadata when present.
    /// </summary>
    public PngBackgroundColor? BackgroundColor;
    /// <summary>
    /// Stores hIST palette frequencies when present.
    /// </summary>
    public PngPaletteHistogram? PaletteHistogram;
    /// <summary>
    /// Stores pHYs physical-pixel metadata when present.
    /// </summary>
    public PngPhysicalPixels? PhysicalPixels;
    /// <summary>
    /// Stores repeatable sPLT suggested palettes in datastream order.
    /// </summary>
    public PngSuggestedPalette[] SuggestedPalettes = new PngSuggestedPalette[0];
    /// <summary>
    /// Stores the eXIf profile when present.
    /// </summary>
    public PngExifProfile? ExifProfile;
    /// <summary>
    /// Stores tIME last-modification metadata when present.
    /// </summary>
    public PngTime? Time;
    /// <summary>
    /// Stores the acTL animation control when present.
    /// </summary>
    public PngAnimationControl? AnimationControl;
    /// <summary>
    /// Stores fcTL frame controls in APNG sequence order.
    /// </summary>
    public PngFrameControl[] FrameControls = new PngFrameControl[0];
    /// <summary>
    /// Stores fdAT compressed frame-data fragments in APNG sequence order.
    /// </summary>
    public PngFrameData[] FrameData = new PngFrameData[0];
    /// <summary>
    /// Stores the combined payload length of metadata chunks.
    /// </summary>
    public long MetadataBytes;
    /// <summary>
    /// Stores the combined decompressed text length used for limit accounting.
    /// </summary>
    public long TextBytes;
    /// <summary>
    /// Stores the number of standard PNG text chunks.
    /// </summary>
    public int TextEntryCount;
}

/// <summary>
/// Reports container and metadata information gathered without decoding image pixels.
/// </summary>
public class PngIdentifyResult
{
    /// <summary>
    /// Initializes a new instance of the PngIdentifyResult class with its documented default values.
    /// </summary>
    public PngIdentifyResult()
    {
    }

    /// <summary>
    /// Indicates whether the expected eight-byte PNG signature was found.
    /// </summary>
    public bool IsPng;
    /// <summary>
    /// Indicates whether the inspected structure satisfied the selected validation policy.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Stores header and animation information found before image decoding.
    /// </summary>
    public PngImageInfo Info = new PngImageInfo();
    /// <summary>
    /// Stores collected metadata and raw chunks.
    /// </summary>
    public PngMetadata Metadata = new PngMetadata();
    /// <summary>
    /// Stores non-fatal structural or metadata issues.
    /// </summary>
    public PngWarning[] Warnings = new PngWarning[0];
    /// <summary>
    /// Stores validation errors encountered during identification.
    /// </summary>
    public PngError[] Errors = new PngError[0];
    /// <summary>
    /// Indicates whether an IEND chunk was reached.
    /// </summary>
    public bool FoundIend;
    /// <summary>
    /// Stores the number of chunks inspected.
    /// </summary>
    public int ChunkCount;
    /// <summary>
    /// Stores the combined IDAT payload length without inflating it.
    /// </summary>
    public long IdatBytes;
}

/// <summary>
/// Supplies replacement payload data for preserved chunks whose types are not recognized by the library.
/// </summary>
public class PngUnknownChunkReplacement
{
    /// <summary>
    /// Initializes an empty unknown-chunk replacement.
    /// </summary>
    public PngUnknownChunkReplacement()
    {
    }

    /// <summary>
    /// Stores the four-character unknown ancillary chunk type to replace.
    /// </summary>
    public string Type = "";
    /// <summary>
    /// Stores the replacement chunk payload without framing bytes.
    /// </summary>
    public byte[] Data = new byte[0];
}

/// <summary>
/// Represents one PNG chunk and the flags derived from its four-character type code.
/// </summary>
public class PngChunk
{
    /// <summary>
    /// Initializes a new instance of the PngChunk class with its documented default values.
    /// </summary>
    public PngChunk()
    {
    }

    /// <summary>
    /// Stores the case-sensitive four-character ASCII chunk type.
    /// </summary>
    public string Type = "";
    /// <summary>
    /// Stores the chunk payload without length, type, or CRC framing bytes.
    /// </summary>
    public byte[] Data = new byte[0];
    /// <summary>
    /// Stores the CRC read from the datastream or calculated for output.
    /// </summary>
    public uint Crc;
    /// <summary>
    /// Stores the original zero-based datastream offset of the chunk length field.
    /// </summary>
    public long Offset;
    /// <summary>
    /// Stores the payload length declared by the chunk framing.
    /// </summary>
    public uint Length;
    /// <summary>
    /// Indicates whether the library recognizes the chunk type.
    /// </summary>
    public bool IsKnown;
    /// <summary>
    /// Indicates whether the chunk type's ancillary bit marks it as critical.
    /// </summary>
    public bool IsCritical;
    /// <summary>
    /// Indicates whether the PNG chunk naming convention marks the type as public rather than private.
    /// </summary>
    public bool IsPublic;
    /// <summary>
    /// Indicates whether the reserved bit in the third type character is zero as required.
    /// </summary>
    public bool IsReservedBitValid;
    /// <summary>
    /// Indicates whether an unknown ancillary chunk may be copied after image data changes.
    /// </summary>
    public bool IsSafeToCopy;
    /// <summary>
    /// Identifies the chunk's position relative to PLTE and the contiguous IDAT region.
    /// </summary>
    public PngChunkPlacement Placement = PngChunkPlacement.Unspecified;
    /// <summary>
    /// Identifies whether the chunk was created by the caller or preserved from an input datastream.
    /// </summary>
    public PngChunkPreservationState PreservationState = PngChunkPreservationState.Created;
}

/// <summary>
/// Describes a non-fatal PNG issue and, when available, its chunk location.
/// </summary>
public class PngWarning
{
    /// <summary>
    /// Initializes a new instance of the PngWarning class with its documented default values.
    /// </summary>
    public PngWarning()
    {
    }

    /// <summary>
    /// Stores a stable machine-readable warning code.
    /// </summary>
    public string Code = "";
    /// <summary>
    /// Stores the developer-readable explanation.
    /// </summary>
    public string Message = "";
    /// <summary>
    /// Stores the related chunk type, or an empty string when the warning is not chunk-specific.
    /// </summary>
    public string ChunkType = "";
    /// <summary>
    /// Stores the related datastream offset, or zero when no offset is available.
    /// </summary>
    public long Offset;
}

/// <summary>
/// Describes a PNG validation error and whether processing can safely continue.
/// </summary>
public class PngError
{
    /// <summary>
    /// Initializes a new instance of the PngError class with its documented default values.
    /// </summary>
    public PngError()
    {
    }

    /// <summary>
    /// Stores a stable machine-readable error code.
    /// </summary>
    public string Code = "";
    /// <summary>
    /// Stores the developer-readable explanation.
    /// </summary>
    public string Message = "";
    /// <summary>
    /// Stores the related chunk type, or an empty string when the error is not chunk-specific.
    /// </summary>
    public string ChunkType = "";
    /// <summary>
    /// Stores the related datastream offset, or zero when no offset is available.
    /// </summary>
    public long Offset;
    /// <summary>
    /// Indicates whether safe parsing or decoding cannot continue.
    /// </summary>
    public bool IsFatal;
}

/// <summary>
/// Reports whether a PNG is valid together with discovered errors, warnings, and image information.
/// </summary>
public class PngValidationResult
{
    /// <summary>
    /// Initializes a new instance of the PngValidationResult class with its documented default values.
    /// </summary>
    public PngValidationResult()
    {
    }

    /// <summary>
    /// Indicates whether no validation errors were found.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Stores header information when a usable IHDR chunk was found.
    /// </summary>
    public PngImageInfo Info = new PngImageInfo();
    /// <summary>
    /// Stores non-fatal issues allowed by the selected policy.
    /// </summary>
    public PngWarning[] Warnings = new PngWarning[0];
    /// <summary>
    /// Stores every validation error collected before processing stopped.
    /// </summary>
    public PngError[] Errors = new PngError[0];
}

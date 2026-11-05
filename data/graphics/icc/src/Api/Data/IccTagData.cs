namespace icc;

/// <summary>
/// Represents the discriminated decoded or raw data stored by one ICC tag.
/// </summary>
public class IccTagData
{
    /// <summary>
    /// Gets or sets the active tag-data representation.
    /// </summary>
    public IccTagDataKind Kind;

    /// <summary>
    /// Gets or sets the lossless raw payload representation.
    /// </summary>
    public IccRawData RawData;

    /// <summary>Gets or sets integer values for integer-array payloads.</summary>
    public long[] IntegerValues;

    /// <summary>Gets or sets decoded values for fixed-point-array payloads.</summary>
    public double[] FixedPointValues;

    /// <summary>Gets or sets decoded values for floating-point-array payloads.</summary>
    public double[] FloatingPointValues;

    /// <summary>Gets or sets encoded XYZ values.</summary>
    public IccXyzNumber[] XyzValues;

    /// <summary>Gets or sets a decoded signature value.</summary>
    public uint Signature;

    /// <summary>Gets or sets an optional decoded date and time.</summary>
    public IccDateTime? DateTime;

    /// <summary>Gets or sets opaque binary payload bytes.</summary>
    public byte[] BinaryData;

    /// <summary>Gets or sets optional text payload data.</summary>
    public IccTextData? Text;

    /// <summary>Gets or sets localized text records.</summary>
    public IccLocalizedString[] LocalizedStrings;

    /// <summary>Gets or sets dictionary metadata entries.</summary>
    public IccMetadataEntry[] Metadata;

    /// <summary>Gets or sets an optional decoded tone curve.</summary>
    public IccCurve? Curve;

    /// <summary>Gets or sets an optional classic lookup table.</summary>
    public IccLut? Lut;

    /// <summary>Gets or sets an optional multi-stage lookup table.</summary>
    public IccMultiStageLut? MultiStageLut;

    /// <summary>Gets or sets an optional multi-process-elements pipeline.</summary>
    public IccMultiProcessElements? MultiProcessElements;

    /// <summary>Gets or sets optional measurement data.</summary>
    public IccMeasurement? Measurement;

    /// <summary>Gets or sets optional viewing conditions.</summary>
    public IccViewingConditions? ViewingConditions;

    /// <summary>Gets or sets optional chromaticity data.</summary>
    public IccChromaticity? Chromaticity;

    /// <summary>Gets or sets optional video coding metadata.</summary>
    public IccCicp? Cicp;

    /// <summary>Gets or sets an optional named-colour set.</summary>
    public IccNamedColorSet? NamedColors;

    /// <summary>Gets or sets an optional colorant set.</summary>
    public IccColorantSet? Colorants;

    /// <summary>Gets or sets response-curve records.</summary>
    public IccResponseCurve[] ResponseCurves;

    /// <summary>Gets or sets the channel count declared by a response-curve-set payload.</summary>
    public int ResponseCurveChannelCount;

    /// <summary>Gets or sets profile-sequence description records.</summary>
    public IccProfileSequenceEntry[] ProfileSequence;

    /// <summary>Gets or sets profile-sequence identifier records.</summary>
    public IccProfileSequenceIdentifier[] ProfileSequenceIdentifiers;

    /// <summary>Gets or sets an optional adaptive-gain curve.</summary>
    public IccAdaptiveGainCurve? AdaptiveGainCurve;

    /// <summary>Gets or sets optional legacy PostScript CRD information.</summary>
    public IccCrdInfo? CrdInfo;

    /// <summary>Gets or sets optional legacy screening parameters.</summary>
    public IccScreening? Screening;

    /// <summary>Gets or sets optional legacy under-colour-removal and black-generation data.</summary>
    public IccUcrBg? UcrBg;

    /// <summary>Gets or sets optional recognized vendor-tag inspection metadata.</summary>
    public IccVendorTagData? VendorData;

    /// <summary>Gets or sets optional spectral data.</summary>
    public IccSpectralData? SpectralData;

    /// <summary>Gets or sets optional spectral viewing conditions.</summary>
    public IccSpectralViewingConditions? SpectralViewingConditions;

    /// <summary>Gets or sets optional ICC.2 device spectral and bi-spectral ranges.</summary>
    public IccSpectralRanges? SpectralRanges;

    /// <summary>Gets or sets an optional ICC.2 profile embedded in an ICC.1 tag.</summary>
    public IccProfile? EmbeddedProfile;

    /// <summary>Gets or sets an optional ICC.2 sparse-matrix array.</summary>
    public IccSparseMatrixArray? SparseMatrixArray;

    /// <summary>Gets or sets an optional hierarchical tag array.</summary>
    public IccTagArray? TagArray;

    /// <summary>Gets or sets an optional hierarchical tag structure.</summary>
    public IccTagStructure? TagStructure;

    /// <summary>Gets or sets an optional gamut-boundary mesh.</summary>
    public IccGamutBoundary? GamutBoundary;

    /// <summary>Gets or sets optional embedded-image data.</summary>
    public IccEmbeddedImage? EmbeddedImage;

    /// <summary>Gets or sets an optional calculator program.</summary>
    public IccCalculatorProgram? Calculator;

    /// <summary>Gets or sets optional abbreviated colour-encoding parameters.</summary>
    public IccColorEncodingParameters? ColorEncodingParameters;

    /// <summary>Gets or sets optional profile connection conditions.</summary>
    public IccProfileConnectionConditions? ProfileConnectionConditions;

    /// <summary>Gets or sets optional multiplex-channel definitions.</summary>
    public IccMultiplexChannels? MultiplexChannels;

    /// <summary>Gets or sets optional bidirectional reflectance data.</summary>
    public IccBrdf? Brdf;

    /// <summary>Gets or sets optional surface-map data.</summary>
    public IccSurfaceMap? SurfaceMap;

    /// <summary>
    /// Initializes tag data with an unassigned kind and an empty raw representation.
    /// </summary>
    public IccTagData()
    {
        Kind = IccTagDataKind.Unknown;
        RawData = new IccRawData();
        IntegerValues = new long[0];
        FixedPointValues = new double[0];
        FloatingPointValues = new double[0];
        XyzValues = new IccXyzNumber[0];
        Signature = 0;
        DateTime = null;
        BinaryData = new byte[0];
        Text = null;
        LocalizedStrings = new IccLocalizedString[0];
        Metadata = new IccMetadataEntry[0];
        Curve = null;
        Lut = null;
        MultiStageLut = null;
        MultiProcessElements = null;
        Measurement = null;
        ViewingConditions = null;
        Chromaticity = null;
        Cicp = null;
        NamedColors = null;
        Colorants = null;
        ResponseCurves = new IccResponseCurve[0];
        ResponseCurveChannelCount = 0;
        ProfileSequence = new IccProfileSequenceEntry[0];
        ProfileSequenceIdentifiers = new IccProfileSequenceIdentifier[0];
        AdaptiveGainCurve = null;
        CrdInfo = null;
        Screening = null;
        UcrBg = null;
        VendorData = null;
        SpectralData = null;
        SpectralViewingConditions = null;
        SpectralRanges = null;
        EmbeddedProfile = null;
        SparseMatrixArray = null;
        TagArray = null;
        TagStructure = null;
        GamutBoundary = null;
        EmbeddedImage = null;
        Calculator = null;
        ColorEncodingParameters = null;
        ProfileConnectionConditions = null;
        MultiplexChannels = null;
        Brdf = null;
        SurfaceMap = null;
    }
}

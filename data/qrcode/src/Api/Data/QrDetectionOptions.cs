namespace qrcode;

/// <summary>Controls image preprocessing, family detection, sampling, and detection resource limits.</summary>
public class QrDetectionOptions
{
    /// <summary>Whether QR Code Model 2 candidates may be detected.</summary>
    public bool AllowModel2 = true;

    /// <summary>Whether Micro QR Code candidates may be detected.</summary>
    public bool AllowMicro = true;

    /// <summary>Whether rectangular Micro QR Code candidates may be detected.</summary>
    public bool AllowRectangularMicro = true;

    /// <summary>Whether legacy QR Code Model 1 candidates may be detected.</summary>
    public bool AllowLegacyModel1 = true;

    /// <summary>The strategy used to convert luminance samples to a binary image.</summary>
    public QrBinarizationMode BinarizationMode = QrBinarizationMode.Automatic;

    /// <summary>The expected image polarity or automatic polarity selection.</summary>
    public QrPolarity Polarity = QrPolarity.Automatic;

    /// <summary>Whether bounded noise removal is enabled before finder scanning.</summary>
    public bool EnableDenoise;

    /// <summary>Whether bounded closing of small binary gaps is enabled.</summary>
    public bool EnableGapClosing;

    /// <summary>Whether luminance contrast normalization is enabled.</summary>
    public bool NormalizeContrast;

    /// <summary>Whether candidates with cropped or absent quiet zones may be returned.</summary>
    public bool AllowCroppedQuietZone = true;

    /// <summary>Whether more than one symbol may be returned from an image.</summary>
    public bool DetectMultiple = true;

    /// <summary>Whether multiple samples within each estimated module may be combined.</summary>
    public bool UseMultiPointSampling = true;

    /// <summary>Whether the sampled grid may be refined against fixed patterns.</summary>
    public bool RefineGrid = true;

    /// <summary>The minimum accepted module size in image pixels.</summary>
    public int MinimumModuleSize = 1;

    /// <summary>The maximum number of finder or symbol candidates retained.</summary>
    public int MaximumCandidates = 256;

    /// <summary>The maximum accepted input image width in pixels.</summary>
    public int MaximumImageWidth = 16384;

    /// <summary>The maximum accepted input image height in pixels.</summary>
    public int MaximumImageHeight = 16384;

    /// <summary>The maximum accepted input image pixel count.</summary>
    public int MaximumImagePixels = 67108864;

    /// <summary>The maximum number of bounded image-pyramid levels.</summary>
    public int MaximumPyramidLevels = 4;
}

namespace raw;

/// <summary>Controls probing and container opening.</summary>
public class RawOpenOptions
{
    /// <summary>Resource bounds for the operation.</summary>
    public RawLimits? Limits;
    /// <summary>The token checked synchronously at bounded work units.</summary>
    public System.Threading.CancellationToken CancellationToken;
    /// <summary>Whether standard and vendor metadata should be read.</summary>
    public bool ReadMetadata;
    /// <summary>Whether embedded preview descriptions and bytes should be read.</summary>
    public bool ReadPreviews;
    /// <summary>Whether raw payload descriptions should be discovered.</summary>
    public bool ReadRawPayload;
    /// <summary>Whether payload bytes should remain deferred until explicitly requested.</summary>
    public bool DeferPayloads;
    /// <summary>Whether named tolerant recovery rules may be used.</summary>
    public bool Tolerant;
    /// <summary>Whether experimental camera profiles may be selected.</summary>
    public bool AllowExperimentalProfiles;
    /// <summary>An optional read-only profile source, or <c>null</c> to use the reviewed built-in profiles.</summary>
    public IRawProfileCatalogue? ProfileCatalogue;
    /// <summary>An optional filename used only for mismatch diagnostics.</summary>
    public string FileNameHint;

    /// <summary>Creates strict open options that defer large payloads.</summary>
    public RawOpenOptions()
    {
        Limits = new RawLimits();
        CancellationToken = default(System.Threading.CancellationToken);
        ReadMetadata = true;
        ReadPreviews = true;
        ReadRawPayload = true;
        DeferPayloads = true;
        Tolerant = false;
        AllowExperimentalProfiles = false;
        ProfileCatalogue = null;
        FileNameHint = string.Empty;
    }

    /// <summary>Validates the open configuration and its resource limits.</summary>
    /// <returns>A report containing all configuration errors.</returns>
    internal RawValidationReport Validate()
    {
        System.Collections.Generic.List<RawValidationMessage> messages = RawOptionValidation.ValidateLimits(Limits);
        if (!ReadMetadata && !ReadPreviews && !ReadRawPayload)
        {
            RawOptionValidation.AddError(messages, "Open.ReadSelection", "At least one input section must be selected.");
        }
        return RawOptionValidation.CreateReport(!Tolerant, messages);
    }

    /// <summary>Throws when synchronous cancellation has been requested.</summary>
    /// <exception cref="System.OperationCanceledException">Cancellation has been requested.</exception>
    internal void CheckCancellation()
    {
        CancellationToken.ThrowIfCancellationRequested();
    }
}

/// <summary>Controls reconstruction of stored raw samples.</summary>
public class RawDecodeOptions
{
    /// <summary>Resource bounds for the operation.</summary>
    public RawLimits? Limits;
    /// <summary>The token checked synchronously at strip, tile, plane, and row boundaries.</summary>
    public System.Threading.CancellationToken CancellationToken;
    /// <summary>The zero-based logical raw image index, excluding previews and thumbnails.</summary>
    public int ImageIndex;
    /// <summary>The zero-based acquisition plane index, where zero selects the primary raw plane.</summary>
    public int PlaneIndex;
    /// <summary>Whether integer samples should be normalized into the sensor-domain representation.</summary>
    public bool NormalizeSamples;
    /// <summary>Whether exact stored samples should be retained with normalized output.</summary>
    public bool PreserveStoredSamples;
    /// <summary>Whether a documented linearization table should be applied.</summary>
    public bool ApplyLinearization;
    /// <summary>Whether named tolerant recovery rules may be used.</summary>
    public bool Tolerant;
    /// <summary>Whether experimental camera profiles may be selected while opening an input for decode.</summary>
    public bool AllowExperimentalProfiles;

    /// <summary>Creates strict normalized decode options for the primary image and plane.</summary>
    public RawDecodeOptions()
    {
        Limits = new RawLimits();
        CancellationToken = default(System.Threading.CancellationToken);
        ImageIndex = 0;
        PlaneIndex = 0;
        NormalizeSamples = true;
        PreserveStoredSamples = false;
        ApplyLinearization = true;
        Tolerant = false;
        AllowExperimentalProfiles = false;
    }

    /// <summary>Validates image selection and resource limits.</summary>
    /// <returns>A report containing all configuration errors.</returns>
    internal RawValidationReport Validate()
    {
        System.Collections.Generic.List<RawValidationMessage> messages = RawOptionValidation.ValidateLimits(Limits);
        RawOptionValidation.AddNonNegative(messages, ImageIndex, "Decode.ImageIndex");
        RawOptionValidation.AddNonNegative(messages, PlaneIndex, "Decode.PlaneIndex");
        return RawOptionValidation.CreateReport(!Tolerant, messages);
    }

    /// <summary>Throws when synchronous cancellation has been requested.</summary>
    /// <exception cref="System.OperationCanceledException">Cancellation has been requested.</exception>
    internal void CheckCancellation()
    {
        CancellationToken.ThrowIfCancellationRequested();
    }
}

/// <summary>Controls deterministic conversion from exact stored samples to normalized sensor values.</summary>
public class RawNormalizationOptions
{
    /// <summary>Resource bounds for normalization and result allocation.</summary>
    public RawLimits Limits;
    /// <summary>The token checked synchronously at row boundaries.</summary>
    public System.Threading.CancellationToken CancellationToken;
    /// <summary>An optional non-destructive crop in stored image coordinates.</summary>
    public RawArea? UserCrop;
    /// <summary>An optional stored-code to linear-value lookup table.</summary>
    public ushort[] LinearizationTable;
    /// <summary>Optional black-level calibration in the linearized sample domain.</summary>
    public RawBlackLevel? BlackLevel;
    /// <summary>Optional scalar or per-channel white levels in the linearized sample domain.</summary>
    public double[] WhiteLevels;
    /// <summary>Masked and optical-black areas retained with the normalized image.</summary>
    public RawArea[] MaskedAreas;
    /// <summary>Whether optical-black areas replace supplied metadata black calibration with an estimate.</summary>
    public bool UseOpticalBlack;
    /// <summary>The horizontal repeat size estimated from optical-black samples.</summary>
    public int OpticalBlackRepeatWidth;
    /// <summary>The vertical repeat size estimated from optical-black samples.</summary>
    public int OpticalBlackRepeatHeight;
    /// <summary>Whether to estimate an additive residual for every stored row.</summary>
    public bool EstimateBlackRowDeltas;
    /// <summary>Whether to estimate an additive residual for every stored column.</summary>
    public bool EstimateBlackColumnDeltas;
    /// <summary>Whether masked samples estimate and remove an additive offset from every stored row.</summary>
    public bool ApplyRowBandingCorrection;
    /// <summary>Whether masked samples estimate and remove an additive offset from every stored column.</summary>
    public bool ApplyColumnBandingCorrection;
    /// <summary>The largest permitted absolute combined banding correction in the linearized stored-sample domain.</summary>
    public double MaximumBandingCorrection;
    /// <summary>Whether to apply <see cref="LinearizationTable"/> before black and white processing.</summary>
    public bool ApplyLinearization;
    /// <summary>Whether to subtract the calibrated black level.</summary>
    public bool SubtractBlackLevel;
    /// <summary>Whether to scale each sample using its calibrated white level.</summary>
    public bool ScaleWhiteLevel;
    /// <summary>Whether the result retains the exact input <see cref="RawStoredImage"/>.</summary>
    public bool PreserveOriginalValues;

    /// <summary>Creates strict full normalization settings that retain exact input values.</summary>
    public RawNormalizationOptions()
    {
        Limits = new RawLimits();
        CancellationToken = default(System.Threading.CancellationToken);
        UserCrop = null;
        LinearizationTable = System.Array.Empty<ushort>();
        BlackLevel = null;
        WhiteLevels = System.Array.Empty<double>();
        MaskedAreas = System.Array.Empty<RawArea>();
        UseOpticalBlack = false;
        OpticalBlackRepeatWidth = 1;
        OpticalBlackRepeatHeight = 1;
        EstimateBlackRowDeltas = false;
        EstimateBlackColumnDeltas = false;
        ApplyRowBandingCorrection = false;
        ApplyColumnBandingCorrection = false;
        MaximumBandingCorrection = double.MaxValue;
        ApplyLinearization = true;
        SubtractBlackLevel = true;
        ScaleWhiteLevel = true;
        PreserveOriginalValues = true;
    }

    /// <summary>Validates resource limits and required option collections.</summary>
    /// <returns>A report containing every configuration error.</returns>
    internal RawValidationReport Validate()
    {
        System.Collections.Generic.List<RawValidationMessage> messages = RawOptionValidation.ValidateLimits(Limits);
        if (LinearizationTable == null)
        {
            RawOptionValidation.AddError(messages, "Normalization.LinearizationTable", "The linearization table collection cannot be null.");
        }
        if (WhiteLevels == null)
        {
            RawOptionValidation.AddError(messages, "Normalization.WhiteLevels", "The white-level collection cannot be null.");
        }
        if (MaskedAreas == null)
        {
            RawOptionValidation.AddError(messages, "Normalization.MaskedAreas", "The masked-area collection cannot be null.");
        }
        if (OpticalBlackRepeatWidth <= 0 || OpticalBlackRepeatHeight <= 0)
        {
            RawOptionValidation.AddError(messages, "Normalization.OpticalBlackRepeat", "Optical-black repeat dimensions must be positive.");
        }
        if (!RawModelValidation.IsFinite(MaximumBandingCorrection) || MaximumBandingCorrection < 0.0)
        {
            RawOptionValidation.AddError(messages, "Normalization.MaximumBandingCorrection", "The maximum banding correction must be finite and nonnegative.");
        }
        return RawOptionValidation.CreateReport(true, messages);
    }

    /// <summary>Throws when synchronous cancellation has been requested.</summary>
    /// <exception cref="System.OperationCanceledException">Cancellation has been requested.</exception>
    internal void CheckCancellation()
    {
        CancellationToken.ThrowIfCancellationRequested();
    }
}

/// <summary>Controls bad-sample repair, conservative defect detection, and CFA green equilibrium.</summary>
public class RawDefectOptions
{
    /// <summary>Resource bounds for validation and repair allocation.</summary>
    public RawLimits Limits;
    /// <summary>The token checked synchronously at row boundaries.</summary>
    public System.Threading.CancellationToken CancellationToken;
    /// <summary>Defects declared by camera or container metadata.</summary>
    public RawDefect[] MetadataDefects;
    /// <summary>Additional defects supplied by the caller.</summary>
    public RawDefect[] CallerDefects;
    /// <summary>Whether isolated near-white samples are detected as hot pixels.</summary>
    public bool DetectHotPixels;
    /// <summary>Whether isolated near-black samples are detected as dead pixels.</summary>
    public bool DetectDeadPixels;
    /// <summary>The minimum normalized value considered by hot-pixel detection.</summary>
    public double HotPixelMinimum;
    /// <summary>The maximum normalized value considered by dead-pixel detection.</summary>
    public double DeadPixelMaximum;
    /// <summary>The minimum normalized separation from the local median required for detection.</summary>
    public double DetectionMinimumDifference;
    /// <summary>The local median absolute deviation multiplier required for detection.</summary>
    public double DetectionNoiseMultiplier;
    /// <summary>Whether declared row defects are repaired.</summary>
    public bool RepairRows;
    /// <summary>Whether declared column defects are repaired.</summary>
    public bool RepairColumns;
    /// <summary>Whether small mean differences between CFA green phases are equalized.</summary>
    public bool ApplyGreenEquilibrium;
    /// <summary>The largest relative green-phase difference that may be corrected.</summary>
    public double GreenEquilibriumMaximumRelativeDifference;

    /// <summary>Creates conservative repair settings with automatic detection and green equilibrium disabled.</summary>
    public RawDefectOptions()
    {
        Limits = new RawLimits();
        CancellationToken = default(System.Threading.CancellationToken);
        MetadataDefects = System.Array.Empty<RawDefect>();
        CallerDefects = System.Array.Empty<RawDefect>();
        DetectHotPixels = false;
        DetectDeadPixels = false;
        HotPixelMinimum = 0.98;
        DeadPixelMaximum = 0.02;
        DetectionMinimumDifference = 0.25;
        DetectionNoiseMultiplier = 8.0;
        RepairRows = true;
        RepairColumns = true;
        ApplyGreenEquilibrium = false;
        GreenEquilibriumMaximumRelativeDifference = 0.1;
    }

    /// <summary>Validates resource limits, collections, and normalized detection thresholds.</summary>
    /// <returns>A report containing every configuration error.</returns>
    internal RawValidationReport Validate()
    {
        System.Collections.Generic.List<RawValidationMessage> messages = RawOptionValidation.ValidateLimits(Limits);
        if (MetadataDefects == null)
        {
            RawOptionValidation.AddError(messages, "Defects.MetadataDefects", "The metadata defect collection cannot be null.");
        }
        if (CallerDefects == null)
        {
            RawOptionValidation.AddError(messages, "Defects.CallerDefects", "The caller defect collection cannot be null.");
        }
        if (!RawModelValidation.IsFinite(HotPixelMinimum) || HotPixelMinimum <= 0.0 || HotPixelMinimum > 1.0)
        {
            RawOptionValidation.AddError(messages, "Defects.HotPixelMinimum", "The hot-pixel minimum must be finite, positive, and no greater than one.");
        }
        if (!RawModelValidation.IsFinite(DeadPixelMaximum) || DeadPixelMaximum < 0.0 || DeadPixelMaximum >= 1.0)
        {
            RawOptionValidation.AddError(messages, "Defects.DeadPixelMaximum", "The dead-pixel maximum must be finite, nonnegative, and less than one.");
        }
        if (RawModelValidation.IsFinite(HotPixelMinimum) && RawModelValidation.IsFinite(DeadPixelMaximum) && DeadPixelMaximum >= HotPixelMinimum)
        {
            RawOptionValidation.AddError(messages, "Defects.DetectionRange", "The dead-pixel maximum must be less than the hot-pixel minimum.");
        }
        if (!RawModelValidation.IsFinite(DetectionMinimumDifference) || DetectionMinimumDifference <= 0.0 || DetectionMinimumDifference > 1.0)
        {
            RawOptionValidation.AddError(messages, "Defects.DetectionMinimumDifference", "The minimum detection difference must be finite, positive, and no greater than one.");
        }
        if (!RawModelValidation.IsFinite(DetectionNoiseMultiplier) || DetectionNoiseMultiplier < 1.0)
        {
            RawOptionValidation.AddError(messages, "Defects.DetectionNoiseMultiplier", "The detection noise multiplier must be finite and at least one.");
        }
        if (!RawModelValidation.IsFinite(GreenEquilibriumMaximumRelativeDifference) || GreenEquilibriumMaximumRelativeDifference < 0.0 || GreenEquilibriumMaximumRelativeDifference > 1.0)
        {
            RawOptionValidation.AddError(messages, "Defects.GreenEquilibriumMaximumRelativeDifference", "The green-equilibrium limit must be finite and between zero and one.");
        }
        return RawOptionValidation.CreateReport(true, messages);
    }

    /// <summary>Throws when synchronous cancellation has been requested.</summary>
    /// <exception cref="System.OperationCanceledException">Cancellation has been requested.</exception>
    internal void CheckCancellation()
    {
        CancellationToken.ThrowIfCancellationRequested();
    }
}

/// <summary>Controls embedded gain-map and raw chromatic-aberration correction.</summary>
public class RawLensCorrectionOptions
{
    /// <summary>Resource bounds for validation and correction allocation.</summary>
    public RawLimits Limits;
    /// <summary>The token checked synchronously at row boundaries.</summary>
    public System.Threading.CancellationToken CancellationToken;
    /// <summary>Whether complete embedded sensor gain maps are applied.</summary>
    public bool ApplyGainMaps;
    /// <summary>Whether complete raw chromatic-aberration metadata is applied before demosaic.</summary>
    public bool ApplyChromaticAberration;

    /// <summary>Creates correction settings with both optional stages disabled.</summary>
    public RawLensCorrectionOptions()
    {
        Limits = new RawLimits();
        CancellationToken = default(System.Threading.CancellationToken);
        ApplyGainMaps = false;
        ApplyChromaticAberration = false;
    }

    /// <summary>Validates correction resource limits.</summary>
    /// <returns>A report containing every configuration error.</returns>
    internal RawValidationReport Validate()
    {
        System.Collections.Generic.List<RawValidationMessage> messages = RawOptionValidation.ValidateLimits(Limits);
        return RawOptionValidation.CreateReport(true, messages);
    }

    /// <summary>Throws when synchronous cancellation has been requested.</summary>
    /// <exception cref="System.OperationCanceledException">Cancellation has been requested.</exception>
    internal void CheckCancellation()
    {
        CancellationToken.ThrowIfCancellationRequested();
    }
}

/// <summary>Controls fast half-size CFA preview reconstruction.</summary>
public class RawPreviewOptions
{
    /// <summary>Resource bounds for validation and preview allocation.</summary>
    public RawLimits Limits;
    /// <summary>The token checked synchronously at output row boundaries.</summary>
    public System.Threading.CancellationToken CancellationToken;

    /// <summary>Creates bounded preview settings.</summary>
    public RawPreviewOptions()
    {
        Limits = new RawLimits();
        CancellationToken = default(System.Threading.CancellationToken);
    }

    /// <summary>Validates preview resource limits.</summary>
    /// <returns>A report containing every configuration error.</returns>
    internal RawValidationReport Validate()
    {
        System.Collections.Generic.List<RawValidationMessage> messages = RawOptionValidation.ValidateLimits(Limits);
        return RawOptionValidation.CreateReport(true, messages);
    }

    /// <summary>Throws when synchronous cancellation has been requested.</summary>
    /// <exception cref="System.OperationCanceledException">Cancellation has been requested.</exception>
    internal void CheckCancellation()
    {
        CancellationToken.ThrowIfCancellationRequested();
    }
}

/// <summary>Controls raw histograms, sample statistics, and optional diagnostic-plane allocation.</summary>
public class RawAnalysisOptions
{
    /// <summary>Resource bounds for validation and analysis allocation.</summary>
    public RawLimits Limits;
    /// <summary>The token checked synchronously at sensor row boundaries.</summary>
    public System.Threading.CancellationToken CancellationToken;
    /// <summary>The histogram bin count spanning normalized values from zero through one.</summary>
    public int HistogramBinCount;
    /// <summary>Whether normalized sample planes are returned.</summary>
    public bool IncludeSamplePlanes;
    /// <summary>Whether available black- and white-clipping planes are returned.</summary>
    public bool IncludeClippingPlanes;
    /// <summary>Whether an available defect-repair plane is returned.</summary>
    public bool IncludeDefectPlane;
    /// <summary>Whether a CFA phase-identifier plane is returned.</summary>
    public bool IncludeCfaPhasePlane;

    /// <summary>Creates 256-bin statistics settings without diagnostic planes.</summary>
    public RawAnalysisOptions()
    {
        Limits = new RawLimits();
        CancellationToken = default(System.Threading.CancellationToken);
        HistogramBinCount = 256;
        IncludeSamplePlanes = false;
        IncludeClippingPlanes = false;
        IncludeDefectPlane = false;
        IncludeCfaPhasePlane = false;
    }

    /// <summary>Validates resource limits and histogram geometry.</summary>
    /// <returns>A report containing every configuration error.</returns>
    internal RawValidationReport Validate()
    {
        System.Collections.Generic.List<RawValidationMessage> messages = RawOptionValidation.ValidateLimits(Limits);
        if (HistogramBinCount < 2 || HistogramBinCount > 65536)
        {
            RawOptionValidation.AddError(messages, "Analysis.HistogramBinCount", "The histogram bin count must be between two and 65536.");
        }
        return RawOptionValidation.CreateReport(true, messages);
    }

    /// <summary>Throws when synchronous cancellation has been requested.</summary>
    /// <exception cref="System.OperationCanceledException">Cancellation has been requested.</exception>
    internal void CheckCancellation()
    {
        CancellationToken.ThrowIfCancellationRequested();
    }
}

/// <summary>Controls bounded per-channel histograms and percentile estimates for raw or rendered samples.</summary>
public class RawHistogramOptions
{
    /// <summary>Resource bounds for validation and histogram allocation.</summary>
    public RawLimits Limits;
    /// <summary>The token checked synchronously at image row boundaries.</summary>
    public System.Threading.CancellationToken CancellationToken;
    /// <summary>An optional region in current image coordinates, or <c>null</c> for the applicable complete or selected image area.</summary>
    public RawArea? Area;
    /// <summary>The fixed number of histogram bins allocated per logical channel.</summary>
    public int HistogramBinCount;
    /// <summary>Requested percentile fractions between zero and one, in caller-specified order.</summary>
    public double[] Percentiles;

    /// <summary>Creates 256-bin settings requesting the minimum, median, and maximum.</summary>
    public RawHistogramOptions()
    {
        Limits = new RawLimits();
        CancellationToken = default(System.Threading.CancellationToken);
        Area = null;
        HistogramBinCount = 256;
        Percentiles = new double[] { 0.0, 0.5, 1.0 };
    }

    /// <summary>Validates resource limits, histogram geometry, region shape, and percentile fractions.</summary>
    /// <returns>A report containing every configuration error.</returns>
    internal RawValidationReport Validate()
    {
        System.Collections.Generic.List<RawValidationMessage> messages = RawOptionValidation.ValidateLimits(Limits);
        if (HistogramBinCount < 2 || HistogramBinCount > 65536)
        {
            RawOptionValidation.AddError(messages, "Histogram.HistogramBinCount", "The histogram bin count must be between two and 65536.");
        }
        if (Area != null && (Area.Kind <= RawAreaKind.Unknown || Area.Kind > RawAreaKind.OpticalBlack || Area.X < 0 || Area.Y < 0 || Area.Width <= 0 || Area.Height <= 0))
        {
            RawOptionValidation.AddError(messages, "Histogram.Area", "The histogram area must be a nonempty defined region with nonnegative coordinates.");
        }
        if (Percentiles == null || Percentiles.Length > 256)
        {
            RawOptionValidation.AddError(messages, "Histogram.Percentiles", "The percentile collection cannot be null or contain more than 256 entries.");
        }
        else
        {
            int index = 0;
            while (index < Percentiles.Length)
            {
                if (!RawModelValidation.IsFinite(Percentiles[index]) || Percentiles[index] < 0.0 || Percentiles[index] > 1.0)
                {
                    RawOptionValidation.AddError(messages, "Histogram.Percentiles", "Every percentile fraction must be finite and between zero and one.");
                    break;
                }
                index++;
            }
        }
        return RawOptionValidation.CreateReport(true, messages);
    }

    /// <summary>Throws when synchronous cancellation has been requested.</summary>
    /// <exception cref="System.OperationCanceledException">Cancellation has been requested.</exception>
    internal void CheckCancellation()
    {
        CancellationToken.ThrowIfCancellationRequested();
    }
}

/// <summary>Controls deterministic conversion from sensor samples to an output image.</summary>
public class RawRenderOptions
{
    /// <summary>Resource bounds for the operation.</summary>
    public RawLimits Limits;
    /// <summary>The token checked synchronously at bounded processing work units.</summary>
    public System.Threading.CancellationToken CancellationToken;
    /// <summary>The zero-based logical raw image index, excluding previews and thumbnails.</summary>
    public int ImageIndex;
    /// <summary>The zero-based acquisition plane index, where zero selects the primary raw plane.</summary>
    public int PlaneIndex;
    /// <summary>An optional non-destructive crop in stored sensor coordinates.</summary>
    public RawArea? UserCrop;
    /// <summary>The demosaic algorithm.</summary>
    public RawDemosaicMode DemosaicMode;
    /// <summary>The white-balance source.</summary>
    public RawWhiteBalanceMode WhiteBalanceMode;
    /// <summary>Caller-supplied daylight, user-multiplier, or temperature-and-tint calibration, or <c>null</c>.</summary>
    public RawWhiteBalance? WhiteBalance;
    /// <summary>An optional auto-white-balance region in reconstructed-image coordinates, or <c>null</c> for the complete image.</summary>
    public RawArea? WhiteBalanceArea;
    /// <summary>The highlight handling mode.</summary>
    public RawHighlightMode HighlightMode;
    /// <summary>The output color space.</summary>
    public RawColorSpace ColorSpace;
    /// <summary>The chromatic adaptation transform used for XYZ and output-space conversion.</summary>
    public RawChromaticAdaptationMode ChromaticAdaptation;
    /// <summary>The output transfer function.</summary>
    public RawTransferFunction TransferFunction;
    /// <summary>The resize interpolation kernel.</summary>
    public RawResampler Resampler;
    /// <summary>The output component depth: <c>8</c>, <c>16</c>, or <c>32</c> for floating point.</summary>
    public int OutputBitDepth;
    /// <summary>The requested output width, or <c>0</c> to retain the current dimensions.</summary>
    public int OutputWidth;
    /// <summary>The requested output height, or <c>0</c> to retain the current dimensions.</summary>
    public int OutputHeight;
    /// <summary>The scene-linear exposure adjustment in stops.</summary>
    public double ExposureCompensation;
    /// <summary>Whether image content selects an additional scene-linear brightness scale.</summary>
    public bool AutoBrightness;
    /// <summary>The fraction of brightest pixels that automatic brightness and saturation protection may place above the unit boundary.</summary>
    public double AutoBrightnessClippingThreshold;
    /// <summary>The multiplicative shadow adjustment, tapered to unity at the unit boundary.</summary>
    public double ShadowScale;
    /// <summary>The strength from zero through one with which per-channel percentiles constrain exposure.</summary>
    public double SaturationProtection;
    /// <summary>The managed post-demosaic noise-reduction method.</summary>
    public RawDenoiseMode DenoiseMode;
    /// <summary>The noise-reduction strength from zero through one.</summary>
    public double DenoiseStrength;
    /// <summary>The post-demosaic false-color suppression strength from zero through one.</summary>
    public double FalseColorSuppression;
    /// <summary>Whether supplied chromatic-aberration calibration is applied after demosaic.</summary>
    public bool ApplyPostDemosaicChromaticAberration;
    /// <summary>The scene-linear capture-sharpening amount from zero through four.</summary>
    public double CaptureSharpening;
    /// <summary>Equally spaced normalized output samples for a monotonic tone curve, or an empty array for identity.</summary>
    public double[] ToneCurve;
    /// <summary>The tone-domain contrast multiplier around middle gray, from zero through four.</summary>
    public double Contrast;
    /// <summary>How normalized RGB values outside the output gamut are handled.</summary>
    public RawGamutMode GamutMode;
    /// <summary>The power-law exponent used when <see cref="TransferFunction"/> is <see cref="RawTransferFunction.Gamma"/>.</summary>
    public double Gamma;
    /// <summary>Whether deterministic four-by-four ordered dithering is applied during eight-bit conversion.</summary>
    public bool OrderedDithering;
    /// <summary>Whether to use the half-size CFA preview path.</summary>
    public bool HalfSize;
    /// <summary>Whether to bake the selected image orientation into output pixels.</summary>
    public bool ApplyOrientation;

    /// <summary>Creates conservative sRGB rendering options using bilinear demosaic and resampling.</summary>
    public RawRenderOptions()
    {
        Limits = new RawLimits();
        CancellationToken = default(System.Threading.CancellationToken);
        ImageIndex = 0;
        PlaneIndex = 0;
        UserCrop = null;
        DemosaicMode = RawDemosaicMode.Bilinear;
        WhiteBalanceMode = RawWhiteBalanceMode.Camera;
        WhiteBalance = null;
        WhiteBalanceArea = null;
        HighlightMode = RawHighlightMode.Clip;
        ColorSpace = RawColorSpace.Srgb;
        ChromaticAdaptation = RawChromaticAdaptationMode.Bradford;
        TransferFunction = RawTransferFunction.Srgb;
        Resampler = RawResampler.Bilinear;
        OutputBitDepth = 8;
        OutputWidth = 0;
        OutputHeight = 0;
        ExposureCompensation = 0.0;
        AutoBrightness = false;
        AutoBrightnessClippingThreshold = 0.01;
        ShadowScale = 1.0;
        SaturationProtection = 0.0;
        DenoiseMode = RawDenoiseMode.None;
        DenoiseStrength = 0.5;
        FalseColorSuppression = 0.0;
        ApplyPostDemosaicChromaticAberration = false;
        CaptureSharpening = 0.0;
        ToneCurve = System.Array.Empty<double>();
        Contrast = 1.0;
        GamutMode = RawGamutMode.None;
        Gamma = 2.2;
        OrderedDithering = false;
        HalfSize = false;
        ApplyOrientation = true;
    }

    /// <summary>Validates render modes, output geometry, numeric controls, and resource limits.</summary>
    /// <returns>A report containing all configuration errors.</returns>
    internal RawValidationReport Validate()
    {
        System.Collections.Generic.List<RawValidationMessage> messages = RawOptionValidation.ValidateLimits(Limits);
        RawOptionValidation.AddNonNegative(messages, ImageIndex, "Render.ImageIndex");
        RawOptionValidation.AddNonNegative(messages, PlaneIndex, "Render.PlaneIndex");
        if (UserCrop != null && (UserCrop.Kind != RawAreaKind.UserCrop || UserCrop.X < 0 || UserCrop.Y < 0 || UserCrop.Width <= 0 || UserCrop.Height <= 0))
        {
            RawOptionValidation.AddError(messages, "Render.UserCrop", "The user crop must have user-crop semantics, nonnegative coordinates, and positive dimensions.");
        }
        RawOptionValidation.ValidateDemosaicMode(messages, DemosaicMode);
        RawOptionValidation.ValidateWhiteBalanceMode(messages, WhiteBalanceMode);
        RawOptionValidation.ValidateWhiteBalanceConfiguration(messages, WhiteBalanceMode, WhiteBalance, WhiteBalanceArea);
        RawOptionValidation.ValidateHighlightMode(messages, HighlightMode);
        RawOptionValidation.ValidateColorSpace(messages, ColorSpace);
        RawOptionValidation.ValidateChromaticAdaptation(messages, ChromaticAdaptation);
        RawOptionValidation.ValidateTransferFunction(messages, TransferFunction);
        RawOptionValidation.ValidateResampler(messages, Resampler);

        if (OutputBitDepth != 8 && OutputBitDepth != 16 && OutputBitDepth != 32)
        {
            RawOptionValidation.AddError(messages, "Render.OutputBitDepth", "Output bit depth must be 8, 16, or 32.");
        }
        if (OutputWidth < 0 || OutputHeight < 0)
        {
            RawOptionValidation.AddError(messages, "Render.OutputDimensions", "Output dimensions cannot be negative.");
        }
        if ((OutputWidth == 0 && OutputHeight != 0) || (OutputWidth != 0 && OutputHeight == 0))
        {
            RawOptionValidation.AddError(messages, "Render.OutputDimensions", "Output width and height must both be zero or both be positive.");
        }
        if (Limits != null && (OutputWidth > Limits.MaxWidth || OutputHeight > Limits.MaxHeight))
        {
            RawOptionValidation.AddError(messages, "Render.OutputDimensions", "Output dimensions exceed the configured limits.");
        }
        if (double.IsNaN(ExposureCompensation) || double.IsInfinity(ExposureCompensation))
        {
            RawOptionValidation.AddError(messages, "Render.ExposureCompensation", "Exposure compensation must be finite.");
        }
        if (double.IsNaN(AutoBrightnessClippingThreshold) || double.IsInfinity(AutoBrightnessClippingThreshold) || AutoBrightnessClippingThreshold < 0.0 || AutoBrightnessClippingThreshold >= 1.0)
        {
            RawOptionValidation.AddError(messages, "Render.AutoBrightnessClippingThreshold", "The automatic-brightness clipping threshold must be finite and at least zero but less than one.");
        }
        if (double.IsNaN(ShadowScale) || double.IsInfinity(ShadowScale) || ShadowScale <= 0.0)
        {
            RawOptionValidation.AddError(messages, "Render.ShadowScale", "The shadow scale must be finite and greater than zero.");
        }
        if (double.IsNaN(SaturationProtection) || double.IsInfinity(SaturationProtection) || SaturationProtection < 0.0 || SaturationProtection > 1.0)
        {
            RawOptionValidation.AddError(messages, "Render.SaturationProtection", "Saturation protection must be finite and between zero and one.");
        }
        if (DenoiseMode < RawDenoiseMode.None || DenoiseMode > RawDenoiseMode.WaveletLike)
        {
            RawOptionValidation.AddError(messages, "Render.DenoiseMode", "The denoise mode is not defined.");
        }
        if (double.IsNaN(DenoiseStrength) || double.IsInfinity(DenoiseStrength) || DenoiseStrength < 0.0 || DenoiseStrength > 1.0)
        {
            RawOptionValidation.AddError(messages, "Render.DenoiseStrength", "Denoise strength must be finite and between zero and one.");
        }
        if (double.IsNaN(FalseColorSuppression) || double.IsInfinity(FalseColorSuppression) || FalseColorSuppression < 0.0 || FalseColorSuppression > 1.0)
        {
            RawOptionValidation.AddError(messages, "Render.FalseColorSuppression", "False-color suppression must be finite and between zero and one.");
        }
        if (double.IsNaN(CaptureSharpening) || double.IsInfinity(CaptureSharpening) || CaptureSharpening < 0.0 || CaptureSharpening > 4.0)
        {
            RawOptionValidation.AddError(messages, "Render.CaptureSharpening", "Capture sharpening must be finite and between zero and four.");
        }
        if (ToneCurve == null)
        {
            RawOptionValidation.AddError(messages, "Render.ToneCurve", "The tone-curve sample collection cannot be null.");
        }
        else if (ToneCurve.Length == 1)
        {
            RawOptionValidation.AddError(messages, "Render.ToneCurve", "A nonempty tone curve requires at least two samples.");
        }
        else
        {
            int toneIndex = 0;
            double previousToneValue = 0.0;
            while (toneIndex < ToneCurve.Length)
            {
                double toneValue = ToneCurve[toneIndex];
                if (!RawModelValidation.IsFinite(toneValue) || toneValue < 0.0 || toneValue > 1.0 || toneIndex > 0 && toneValue < previousToneValue)
                {
                    RawOptionValidation.AddError(messages, "Render.ToneCurve", "Tone-curve samples must be finite, normalized, and monotonically nondecreasing.");
                    break;
                }
                previousToneValue = toneValue;
                toneIndex++;
            }
        }
        if (!RawModelValidation.IsFinite(Contrast) || Contrast < 0.0 || Contrast > 4.0)
        {
            RawOptionValidation.AddError(messages, "Render.Contrast", "Contrast must be finite and between zero and four.");
        }
        if (GamutMode < RawGamutMode.None || GamutMode > RawGamutMode.Compress)
        {
            RawOptionValidation.AddError(messages, "Render.GamutMode", "The gamut-handling mode is not defined.");
        }
        if (double.IsNaN(Gamma) || double.IsInfinity(Gamma) || Gamma <= 0.0)
        {
            RawOptionValidation.AddError(messages, "Render.Gamma", "Gamma must be finite and greater than zero.");
        }

        return RawOptionValidation.CreateReport(true, messages);
    }

    /// <summary>Throws when synchronous cancellation has been requested.</summary>
    /// <exception cref="System.OperationCanceledException">Cancellation has been requested.</exception>
    internal void CheckCancellation()
    {
        CancellationToken.ThrowIfCancellationRequested();
    }
}

/// <summary>Controls managed ICC profile transforms.</summary>
public class RawIccTransformOptions
{
    /// <summary>Resource bounds for the transform.</summary>
    public RawLimits Limits;
    /// <summary>The token checked synchronously at bounded processing work units.</summary>
    public System.Threading.CancellationToken CancellationToken;
    /// <summary>The ICC rendering intent used to select profile transforms.</summary>
    public RawIccRenderingIntent RenderingIntent;

    /// <summary>Creates media-relative ICC transform options.</summary>
    public RawIccTransformOptions()
    {
        Limits = new RawLimits();
        CancellationToken = default(System.Threading.CancellationToken);
        RenderingIntent = RawIccRenderingIntent.MediaRelativeColorimetric;
    }

    /// <summary>Validates resource bounds and the rendering intent.</summary>
    /// <returns>All configuration findings.</returns>
    internal RawValidationReport Validate()
    {
        System.Collections.Generic.List<RawValidationMessage> messages = RawOptionValidation.ValidateLimits(Limits);
        if (RenderingIntent < RawIccRenderingIntent.Perceptual || RenderingIntent > RawIccRenderingIntent.IccAbsoluteColorimetric)
        {
            RawOptionValidation.AddError(messages, "Icc.RenderingIntent", "The ICC rendering intent is not defined.");
        }
        return RawOptionValidation.CreateReport(true, messages);
    }

    /// <summary>Throws when synchronous cancellation has been requested.</summary>
    /// <exception cref="System.OperationCanceledException">Cancellation has been requested.</exception>
    internal void CheckCancellation()
    {
        CancellationToken.ThrowIfCancellationRequested();
    }
}

/// <summary>Controls metadata parsing, preservation, and reconciliation.</summary>
public class RawMetadataOptions
{
    /// <summary>Resource bounds for the operation.</summary>
    public RawLimits? Limits;
    /// <summary>The token checked synchronously at metadata block boundaries.</summary>
    public System.Threading.CancellationToken CancellationToken;
    /// <summary>Whether Exif fields should be read.</summary>
    public bool ReadExif;
    /// <summary>Whether GPS fields should be read.</summary>
    public bool ReadGps;
    /// <summary>Whether XMP packets should be read.</summary>
    public bool ReadXmp;
    /// <summary>Whether IPTC records should be read.</summary>
    public bool ReadIptc;
    /// <summary>Whether ICC profiles should be read.</summary>
    public bool ReadIcc;
    /// <summary>Whether recognized maker-note fields should be decoded.</summary>
    public bool ReadMakerNotes;
    /// <summary>Whether original encoded packets and undecoded bytes should be retained.</summary>
    public bool PreserveRawPackets;
    /// <summary>Whether parallel metadata representations should be reconciled.</summary>
    public bool Reconcile;
    /// <summary>The preferred representation when reconciliation is enabled.</summary>
    public RawMetadataPrecedence Precedence;
    /// <summary>An optional read-only profile source, or <c>null</c> to use the reviewed built-in profiles.</summary>
    public IRawProfileCatalogue? ProfileCatalogue;

    /// <summary>Creates metadata options that read and preserve every supported representation without reconciliation.</summary>
    public RawMetadataOptions()
    {
        Limits = new RawLimits();
        CancellationToken = default(System.Threading.CancellationToken);
        ReadExif = true;
        ReadGps = true;
        ReadXmp = true;
        ReadIptc = true;
        ReadIcc = true;
        ReadMakerNotes = true;
        PreserveRawPackets = true;
        Reconcile = false;
        Precedence = RawMetadataPrecedence.None;
        ProfileCatalogue = null;
    }

    /// <summary>Validates metadata selection, reconciliation, and resource limits.</summary>
    /// <returns>A report containing all configuration errors.</returns>
    internal RawValidationReport Validate()
    {
        System.Collections.Generic.List<RawValidationMessage> messages = RawOptionValidation.ValidateLimits(Limits);
        RawOptionValidation.ValidateMetadataPrecedence(messages, Precedence);
        if (!ReadExif && !ReadGps && !ReadXmp && !ReadIptc && !ReadIcc && !ReadMakerNotes)
        {
            RawOptionValidation.AddError(messages, "Metadata.ReadSelection", "At least one metadata representation must be selected.");
        }
        if (Reconcile && Precedence == RawMetadataPrecedence.None)
        {
            RawOptionValidation.AddError(messages, "Metadata.Precedence", "Reconciliation requires an explicit metadata precedence.");
        }
        if (!Reconcile && Precedence != RawMetadataPrecedence.None)
        {
            RawOptionValidation.AddError(messages, "Metadata.Precedence", "Metadata precedence applies only when reconciliation is enabled.");
        }
        return RawOptionValidation.CreateReport(true, messages);
    }

    /// <summary>Throws when synchronous cancellation has been requested.</summary>
    /// <exception cref="System.OperationCanceledException">Cancellation has been requested.</exception>
    internal void CheckCancellation()
    {
        CancellationToken.ThrowIfCancellationRequested();
    }
}

/// <summary>Controls new-file writing and preserving rewrites.</summary>
public class RawWriteOptions
{
    /// <summary>Resource bounds for planning, buffering, and output validation.</summary>
    public RawLimits Limits;
    /// <summary>The token checked synchronously at block and row boundaries.</summary>
    public System.Threading.CancellationToken CancellationToken;
    /// <summary>The requested write operation.</summary>
    public RawWriteMode Mode;
    /// <summary>The policy for unknown or private data.</summary>
    public RawUnknownDataPolicy UnknownDataPolicy;
    /// <summary>The selected profile name, or an empty string for profile matching.</summary>
    public string ProfileName;
    /// <summary>Whether experimental write profiles may be selected.</summary>
    public bool AllowExperimentalProfiles;
    /// <summary>Whether an explicitly lossy encoder may be selected.</summary>
    public bool AllowLossy;
    /// <summary>Whether the caller consents to every data-loss action reported by the plan.</summary>
    public bool DataLossConsent;
    /// <summary>Whether output must be reopened and structurally validated before commit.</summary>
    public bool ValidateAfterWrite;
    /// <summary>Whether lossless output must pass decoded pixel-identity verification.</summary>
    public bool VerifyPixelIdentity;
    /// <summary>Whether metadata-only rewrites must verify compressed raw payload byte identity after reopening.</summary>
    public bool VerifyRawPayloadIdentity;

    /// <summary>Creates preserving, lossless, post-validated write options.</summary>
    public RawWriteOptions()
    {
        Limits = new RawLimits();
        CancellationToken = default(System.Threading.CancellationToken);
        Mode = RawWriteMode.PreservingRewrite;
        UnknownDataPolicy = RawUnknownDataPolicy.Preserve;
        ProfileName = string.Empty;
        AllowExperimentalProfiles = false;
        AllowLossy = false;
        DataLossConsent = false;
        ValidateAfterWrite = true;
        VerifyPixelIdentity = true;
        VerifyRawPayloadIdentity = true;
    }

    /// <summary>Validates write safety controls and resource limits.</summary>
    /// <returns>A report containing all configuration errors.</returns>
    internal RawValidationReport Validate()
    {
        System.Collections.Generic.List<RawValidationMessage> messages = RawOptionValidation.ValidateLimits(Limits);
        RawOptionValidation.ValidateWriteMode(messages, Mode);
        RawOptionValidation.ValidateUnknownDataPolicy(messages, UnknownDataPolicy);
        if (UnknownDataPolicy == RawUnknownDataPolicy.DropWithConsent && !DataLossConsent)
        {
            RawOptionValidation.AddError(messages, "Write.DataLossConsent", "Dropping unknown data requires explicit consent.");
        }
        if (AllowLossy && !DataLossConsent)
        {
            RawOptionValidation.AddError(messages, "Write.DataLossConsent", "Lossy encoding requires explicit consent.");
        }
        if (!ValidateAfterWrite)
        {
            RawOptionValidation.AddError(messages, "Write.ValidateAfterWrite", "Post-write validation cannot be disabled.");
        }
        return RawOptionValidation.CreateReport(true, messages);
    }

    /// <summary>Throws when synchronous cancellation has been requested.</summary>
    /// <exception cref="System.OperationCanceledException">Cancellation has been requested.</exception>
    internal void CheckCancellation()
    {
        CancellationToken.ThrowIfCancellationRequested();
    }
}

/// <summary>Controls conformance validation and tolerant recovery.</summary>
public class RawValidationOptions
{
    /// <summary>Resource bounds for the validation operation.</summary>
    public RawLimits Limits;
    /// <summary>The token checked synchronously at bounded validation work units.</summary>
    public System.Threading.CancellationToken CancellationToken;
    /// <summary>Whether every non-conformance is rejected.</summary>
    public bool Strict;
    /// <summary>Whether named recovery rules may be applied.</summary>
    public bool AllowRecovery;
    /// <summary>Whether container structure is validated.</summary>
    public bool ValidateStructure;
    /// <summary>Whether compressed and packed payload rules are validated.</summary>
    public bool ValidateCodec;
    /// <summary>Whether image and metadata semantics are validated.</summary>
    public bool ValidateSemantic;
    /// <summary>Whether camera and writer profile conformance is validated.</summary>
    public bool ValidateProfile;
    /// <summary>Whether metadata representations are validated.</summary>
    public bool ValidateMetadata;
    /// <summary>Whether output layout and write conformance are validated.</summary>
    public bool ValidateWriter;

    /// <summary>Creates strict validation options covering every subsystem.</summary>
    public RawValidationOptions()
    {
        Limits = new RawLimits();
        CancellationToken = default(System.Threading.CancellationToken);
        Strict = true;
        AllowRecovery = false;
        ValidateStructure = true;
        ValidateCodec = true;
        ValidateSemantic = true;
        ValidateProfile = true;
        ValidateMetadata = true;
        ValidateWriter = true;
    }

    /// <summary>Validates subsystem selection, recovery policy, and resource limits.</summary>
    /// <returns>A report containing all configuration errors.</returns>
    internal RawValidationReport Validate()
    {
        System.Collections.Generic.List<RawValidationMessage> messages = RawOptionValidation.ValidateLimits(Limits);
        if (!ValidateStructure && !ValidateCodec && !ValidateSemantic && !ValidateProfile && !ValidateMetadata && !ValidateWriter)
        {
            RawOptionValidation.AddError(messages, "Validation.Selection", "At least one validation subsystem must be selected.");
        }
        if (Strict && AllowRecovery)
        {
            RawOptionValidation.AddError(messages, "Validation.AllowRecovery", "Strict validation cannot apply tolerant recovery rules.");
        }
        return RawValidationReport.CreateSelected(Strict, messages.ToArray(), ValidateStructure, ValidateCodec, ValidateSemantic, ValidateProfile, ValidateMetadata, ValidateWriter);
    }

    /// <summary>Throws when synchronous cancellation has been requested.</summary>
    /// <exception cref="System.OperationCanceledException">Cancellation has been requested.</exception>
    internal void CheckCancellation()
    {
        CancellationToken.ThrowIfCancellationRequested();
    }
}

namespace raw;

/// <summary>Provides deterministic raw preprocessing and rendering pipeline entry points.</summary>
internal static class RawRenderer
{
    /// <summary>Renders a normalized sensor image through reconstruction, white balance, detail, exposure, color, tone, geometry, and output conversion stages.</summary>
    /// <param name="sensorImage">A normalized monochrome, linear RGB, or CFA sensor image with applicable calibration.</param>
    /// <param name="options">All reconstruction, color, tone, geometry, output, cancellation, and resource controls.</param>
    /// <returns>A tightly packed deterministic gray, RGB, or RGBA output image.</returns>
    /// <remarks>Profile interpolation uses caller white-balance temperature when supplied, then camera white-balance temperature, then 6500 K. Individual stage methods remain available for explicit XYZ or ICC workflows.</remarks>
    /// <exception cref="RawFormatException">The sensor image, options, calibration, samples, or output geometry are invalid.</exception>
    /// <exception cref="RawProfileException">A required camera matrix profile is missing, invalid, or ambiguous.</exception>
    /// <exception cref="RawUnsupportedException">The requested stage combination or representation is unsupported.</exception>
    public static RawRgbImage Render(RawSensorImage sensorImage, RawRenderOptions options)
    {
        return RawRenderPipeline.Render(sensorImage, options);
    }

    /// <summary>Estimates arithmetic-mean repeating, per-channel black calibration and optional row or column residuals from optical-black areas.</summary>
    /// <param name="storedImage">The exact decoded stored samples.</param>
    /// <param name="options">Optical areas, repeat geometry, linearization, cancellation, and resource controls.</param>
    /// <returns>Black calibration in the post-linearization sample domain.</returns>
    /// <exception cref="RawFormatException">The image, optical areas, repeat coverage, or samples are invalid.</exception>
    public static RawBlackLevel EstimateBlackLevel(RawStoredImage storedImage, RawNormalizationOptions options)
    {
        RawNormalizer.Validate(storedImage, options);
        return RawOpticalBlackEstimator.EstimateValidated(storedImage, options);
    }

    /// <summary>Normalizes exact stored values through linearization, black subtraction, white scaling, crop modeling, and clipping detection.</summary>
    /// <param name="storedImage">The exact decoded stored samples.</param>
    /// <param name="options">Calibration, crop, retention, cancellation, and resource controls.</param>
    /// <returns>A full-size floating sensor image with a non-destructive crop model and clipping masks.</returns>
    /// <exception cref="RawFormatException">The image, calibration, crop, or sample values are invalid.</exception>
    /// <exception cref="RawUnsupportedException">The requested linearization is incompatible with the stored sample representation.</exception>
    public static RawSensorImage Preprocess(RawStoredImage storedImage, RawNormalizationOptions options)
    {
        return RawNormalizer.Normalize(storedImage, options);
    }

    /// <summary>Normalizes retained exact values from a decoded sensor image, using its calibration when options omit it.</summary>
    /// <param name="sensorImage">A decoded image that retains exact stored values.</param>
    /// <param name="options">Calibration overrides, crop, retention, cancellation, and resource controls.</param>
    /// <returns>A full-size floating sensor image with a non-destructive crop model and clipping masks.</returns>
    /// <exception cref="RawFormatException">Exact stored values are absent or preprocessing inputs are invalid.</exception>
    public static RawSensorImage Preprocess(RawSensorImage sensorImage, RawNormalizationOptions options)
    {
        if (sensorImage == null || sensorImage.StoredImage == null || options == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A decoded sensor image with retained exact values is required for preprocessing.", -1);
        }
        RawNormalizationOptions effective = CopyOptions(options);
        if (effective.BlackLevel == null && !effective.UseOpticalBlack)
        {
            effective.BlackLevel = sensorImage.BlackLevel;
        }
        if (effective.WhiteLevels.Length == 0 && sensorImage.WhiteLevels != null)
        {
            effective.WhiteLevels = sensorImage.WhiteLevels;
        }
        if (effective.MaskedAreas.Length == 0 && sensorImage.MaskedAreas != null)
        {
            effective.MaskedAreas = sensorImage.MaskedAreas;
        }
        RawSensorImage result = RawNormalizer.Normalize(sensorImage.StoredImage, effective);
        result.WhiteBalance = sensorImage.WhiteBalance;
        result.ColorProfiles = sensorImage.ColorProfiles;
        result.GainMaps = sensorImage.GainMaps;
        result.ChromaticAberration = sensorImage.ChromaticAberration;
        result.Diagnostics = sensorImage.Diagnostics;
        return result;
    }

    /// <summary>Repairs declared bad pixels, rows, and columns, optionally detects isolated hot or dead pixels, and can equalize CFA green phases.</summary>
    /// <param name="sensorImage">A normalized sensor image.</param>
    /// <param name="options">Defect declarations, detection thresholds, green equilibrium, cancellation, and resource controls.</param>
    /// <returns>A new floating sensor image with a defect map and the original input left unchanged.</returns>
    /// <exception cref="RawFormatException">The image, declarations, normalized samples, or repair neighborhood are invalid.</exception>
    /// <exception cref="RawUnsupportedException">Green equilibrium is requested for incompatible sensor geometry.</exception>
    public static RawSensorImage RepairDefects(RawSensorImage sensorImage, RawDefectOptions options)
    {
        return RawDefectProcessor.Repair(sensorImage, options);
    }

    /// <summary>Applies complete embedded gain maps and raw chromatic-aberration scaling before demosaic.</summary>
    /// <param name="sensorImage">A normalized sensor image carrying optional embedded lens calibration.</param>
    /// <param name="options">Stage selection, cancellation, and resource controls.</param>
    /// <returns>A new floating sensor image with applied-state flags and the original input left unchanged.</returns>
    /// <exception cref="RawFormatException">The image, calibration, coverage, or normalized samples are invalid.</exception>
    /// <exception cref="RawUnsupportedException">A requested correction lacks complete compatible metadata.</exception>
    public static RawSensorImage CorrectLens(RawSensorImage sensorImage, RawLensCorrectionOptions options)
    {
        return RawLensCorrector.Correct(sensorImage, options);
    }

    /// <summary>Reconstructs one linear RGB pixel per two-by-two Bayer block for fast preview use.</summary>
    /// <param name="sensorImage">A normalized two-by-two Bayer sensor image.</param>
    /// <param name="options">Cancellation and resource controls.</param>
    /// <returns>A crop-aware half-size floating linear camera-RGB image.</returns>
    /// <exception cref="RawFormatException">The image, samples, crop, or available color samples are invalid.</exception>
    /// <exception cref="RawUnsupportedException">The sensor does not use a compatible two-by-two Bayer CFA.</exception>
    public static RawRgbImage CreateHalfSizePreview(RawSensorImage sensorImage, RawPreviewOptions options)
    {
        return RawFastProcessor.CreateHalfSizePreview(sensorImage, options);
    }

    /// <summary>Computes selected-area raw histograms, moments, clipping and repair counts, and requested diagnostic planes.</summary>
    /// <param name="sensorImage">A normalized sensor image.</param>
    /// <param name="options">Histogram geometry, diagnostic-plane selection, cancellation, and resource controls.</param>
    /// <returns>Statistics per logical channel or CFA phase and the requested selected-area planes.</returns>
    /// <exception cref="RawFormatException">The image, options, samples, or allocation geometry are invalid.</exception>
    /// <exception cref="RawUnsupportedException">A requested diagnostic plane lacks its source metadata.</exception>
    public static RawAnalysisResult Analyze(RawSensorImage sensorImage, RawAnalysisOptions options)
    {
        return RawFastProcessor.Analyze(sensorImage, options);
    }

    /// <summary>Computes bounded per-channel histograms and percentile estimates over exact stored raw sample values.</summary>
    /// <param name="storedImage">An exact decoded stored-sample image.</param>
    /// <param name="options">Region, fixed-bin geometry, percentiles, cancellation, and resource controls.</param>
    /// <returns>Per-channel histograms and percentile estimates in the stored numeric domain.</returns>
    /// <remarks>Memory use is proportional to logical channels times the configured bin count, independent of source bit depth and pixel count. Interior percentile values are bounded-bin estimates; zero and one return exact extrema.</remarks>
    /// <exception cref="RawFormatException">The image, options, region, samples, or allocation geometry are invalid.</exception>
    public static RawHistogramResult Histogram(RawStoredImage storedImage, RawHistogramOptions options)
    {
        return RawHistogramProcessor.Calculate(storedImage, options);
    }

    /// <summary>Computes bounded per-channel histograms and percentile estimates over normalized sensor samples.</summary>
    /// <param name="sensorImage">A normalized sensor image.</param>
    /// <param name="options">Region, fixed-bin geometry, percentiles, cancellation, and resource controls.</param>
    /// <returns>Per-channel histograms and percentile estimates in the normalized zero-through-one domain.</returns>
    /// <remarks>When no region is supplied, the resolved non-destructive crop is used. Masked pixels are excluded. Memory use is proportional to logical channels times the configured bin count.</remarks>
    /// <exception cref="RawFormatException">The image, options, region, samples, or allocation geometry are invalid.</exception>
    public static RawHistogramResult Histogram(RawSensorImage sensorImage, RawHistogramOptions options)
    {
        return RawHistogramProcessor.Calculate(sensorImage, options);
    }

    /// <summary>Computes bounded per-channel histograms and percentile estimates over rendered byte, unsigned-short, or floating samples.</summary>
    /// <param name="image">A rendered gray, RGB, or RGBA image.</param>
    /// <param name="options">Region, fixed-bin geometry, percentiles, cancellation, and resource controls.</param>
    /// <returns>Per-channel histograms and percentile estimates in the rendered buffer numeric domain.</returns>
    /// <remarks>Integer histograms span the complete buffer domain; floating histograms span observed per-channel extrema. Memory use is proportional to logical channels times the configured bin count.</remarks>
    /// <exception cref="RawFormatException">The image, options, region, samples, or allocation geometry are invalid.</exception>
    public static RawHistogramResult Histogram(RawRgbImage image, RawHistogramOptions options)
    {
        return RawHistogramProcessor.Calculate(image, options);
    }

    /// <summary>Returns cropped camera-linear samples unchanged or reconstructs a full-size Bayer RGB image with nearest-neighbor, bilinear, edge-aware VNG, or homogeneity-directed AHD interpolation.</summary>
    /// <param name="sensorImage">A normalized monochrome, RGB, linear raw, or CFA sensor image.</param>
    /// <param name="options">The demosaic selection, cancellation, and resource controls.</param>
    /// <returns>A floating camera-linear image retaining unapplied orientation metadata.</returns>
    /// <remarks>This reconstruction stage does not apply white balance, color conversion, resizing, transfer functions, bit-depth conversion, or orientation.</remarks>
    /// <exception cref="RawFormatException">The image, options, normalized samples, or required source colors are invalid.</exception>
    /// <exception cref="RawUnsupportedException">The image interpretation, channel geometry, CFA, or requested demosaic mode is not supported by this stage.</exception>
    public static RawRgbImage Reconstruct(RawSensorImage sensorImage, RawRenderOptions options)
    {
        return RawDemosaicer.Reconstruct(sensorImage, options);
    }

    /// <summary>Applies normalized camera, daylight, auto, user, or temperature-and-tint multipliers to floating camera-linear RGB samples.</summary>
    /// <param name="image">A reconstructed floating camera-linear RGB or RGBA image.</param>
    /// <param name="cameraWhiteBalance">The camera as-shot calibration used by camera mode, or <c>null</c> for other modes.</param>
    /// <param name="options">The white-balance mode, caller calibration, auto region, cancellation, and resource controls.</param>
    /// <returns>A new floating camera-linear image with RGB channels balanced and alpha unchanged.</returns>
    /// <remarks>Daylight mode uses caller multipliers when supplied and otherwise derives a standard 6500 K balance. Temperature conversion uses a Planckian chromaticity approximation and treats tint as a green-axis exposure-stop adjustment.</remarks>
    /// <exception cref="RawFormatException">The image, options, calibration, region, or samples are invalid.</exception>
    /// <exception cref="RawUnsupportedException">The input is not floating camera-linear RGB or camera calibration is unavailable.</exception>
    public static RawRgbImage ApplyWhiteBalance(RawRgbImage image, RawWhiteBalance? cameraWhiteBalance, RawRenderOptions options)
    {
        return RawWhiteBalanceProcessor.Apply(image, cameraWhiteBalance, options);
    }

    /// <summary>Converts white-balanced floating camera-linear RGB through one to three interpolated matrix profiles into D50 or D65 XYZ.</summary>
    /// <param name="image">A white-balanced floating camera-linear RGB or RGBA image.</param>
    /// <param name="profiles">One to three applicable matrix color profiles, optionally alongside valid non-matrix profiles.</param>
    /// <param name="illuminantTemperatureKelvin">The scene illuminant temperature used for reciprocal-temperature profile interpolation.</param>
    /// <param name="options">The D50 or D65 XYZ target, adaptation mode, cancellation, and resource controls.</param>
    /// <returns>A tightly packed floating XYZ or XYZA image with linear transfer and retained orientation state.</returns>
    /// <exception cref="RawFormatException">The image, options, temperature, or samples are invalid.</exception>
    /// <exception cref="RawProfileException">The matrix profiles, illuminants, white points, or matrix operations are invalid or ambiguous.</exception>
    /// <exception cref="RawUnsupportedException">The input or requested target color space is unsupported by this stage.</exception>
    public static RawRgbImage ConvertCameraToXyz(RawRgbImage image, RawColorProfile[] profiles, double illuminantTemperatureKelvin, RawRenderOptions options)
    {
        return RawCameraColorProcessor.Convert(image, profiles, illuminantTemperatureKelvin, options);
    }

    /// <summary>Converts linear XYZ into a built-in RGB output space, adapts XYZ white points, or preserves raw-camera RGB, then applies the selected transfer function.</summary>
    /// <param name="image">Floating linear raw-camera RGB/RGBA or D50/D65 XYZ/XYZA samples.</param>
    /// <param name="options">The target color space, transfer function, gamma, adaptation mode, cancellation, and resource controls.</param>
    /// <returns>A tightly packed floating image in the requested built-in color space and transfer function.</returns>
    /// <remarks>sRGB and Display P3 share the sRGB transfer option, Rec. 2020 and ProPhoto provide their standard transfer options, Adobe RGB accepts caller gamma, and ACEScg and XYZ remain linear.</remarks>
    /// <exception cref="RawFormatException">The image, options, or samples are invalid.</exception>
    /// <exception cref="RawUnsupportedException">The input, target, or color-space and transfer-function combination is unsupported.</exception>
    public static RawRgbImage ConvertOutputColor(RawRgbImage image, RawRenderOptions options)
    {
        return RawOutputColorProcessor.Convert(image, options);
    }

    /// <summary>Converts ICC-encoded RGB or RGBA samples through an input profile to linear D50 PCS XYZ.</summary>
    /// <param name="image">Floating ICC RGB or RGBA samples.</param>
    /// <param name="profile">The validated RGB input profile.</param>
    /// <param name="options">The rendering intent, cancellation, and resource controls.</param>
    /// <returns>A tightly packed linear D50 XYZ or XYZA image.</returns>
    /// <exception cref="RawFormatException">The image, profile, options, or samples are invalid.</exception>
    /// <exception cref="RawUnsupportedException">The profile color spaces or transform elements are unsupported.</exception>
    public static RawRgbImage ConvertIccToXyz(RawRgbImage image, RawIccProfile profile, RawIccTransformOptions options)
    {
        return RawIccTransformProcessor.ConvertToXyz(image, profile, options);
    }

    /// <summary>Converts linear D50 PCS XYZ or XYZA samples through an output ICC profile.</summary>
    /// <param name="image">Floating linear D50 XYZ or XYZA samples.</param>
    /// <param name="profile">The validated RGB output profile.</param>
    /// <param name="options">The rendering intent, cancellation, and resource controls.</param>
    /// <returns>A tightly packed ICC-encoded RGB or RGBA image.</returns>
    /// <exception cref="RawFormatException">The image, profile, options, or samples are invalid.</exception>
    /// <exception cref="RawUnsupportedException">The profile color spaces or transform elements are unsupported.</exception>
    public static RawRgbImage ConvertXyzToIcc(RawRgbImage image, RawIccProfile profile, RawIccTransformOptions options)
    {
        return RawIccTransformProcessor.ConvertFromXyz(image, profile, options);
    }

    /// <summary>Converts floating ICC RGB or RGBA samples between two RGB profiles through D50 PCS XYZ.</summary>
    /// <param name="image">Floating samples encoded by the input profile.</param>
    /// <param name="inputProfile">The validated RGB input profile.</param>
    /// <param name="outputProfile">The validated RGB output profile.</param>
    /// <param name="options">The rendering intent, cancellation, and resource controls.</param>
    /// <returns>A tightly packed image encoded by the output profile.</returns>
    /// <exception cref="RawFormatException">The image, profiles, options, or samples are invalid.</exception>
    /// <exception cref="RawUnsupportedException">A profile color space or transform element is unsupported.</exception>
    public static RawRgbImage ConvertIcc(RawRgbImage image, RawIccProfile inputProfile, RawIccProfile outputProfile, RawIccTransformOptions options)
    {
        RawRgbImage xyz = RawIccTransformProcessor.ConvertToXyz(image, inputProfile, options);
        return RawIccTransformProcessor.ConvertFromXyz(xyz, outputProfile, options);
    }

    /// <summary>Applies exposure, optional automatic brightness, tapered shadow scaling, per-channel saturation protection, and highlight handling to scene-linear RGB.</summary>
    /// <param name="image">Floating scene-linear RGB or RGBA samples.</param>
    /// <param name="options">Exposure, brightness, shadow, saturation, highlight, cancellation, and resource controls.</param>
    /// <returns>A tightly packed floating scene-linear RGB or RGBA image.</returns>
    /// <exception cref="RawFormatException">The image, options, or samples are invalid.</exception>
    /// <exception cref="RawUnsupportedException">The input representation is unsupported by this stage.</exception>
    public static RawRgbImage ApplyExposureAndHighlights(RawRgbImage image, RawRenderOptions options)
    {
        return RawExposureProcessor.Apply(image, options);
    }

    /// <summary>Applies managed post-demosaic denoise, false-color suppression, optional chromatic-aberration resampling, and capture sharpening.</summary>
    /// <param name="image">Floating scene-linear RGB or RGBA samples.</param>
    /// <param name="chromaticAberration">Optional radial red and blue calibration required when post-demosaic correction is enabled.</param>
    /// <param name="options">Denoise, false-color, chromatic-aberration, sharpening, cancellation, and resource controls.</param>
    /// <returns>A tightly packed floating scene-linear RGB or RGBA image.</returns>
    /// <exception cref="RawFormatException">The image, options, calibration, or samples are invalid.</exception>
    /// <exception cref="RawUnsupportedException">The input representation or requested calibration is unsupported.</exception>
    public static RawRgbImage EnhanceDetails(RawRgbImage image, RawChromaticAberration? chromaticAberration, RawRenderOptions options)
    {
        return RawDetailProcessor.Apply(image, chromaticAberration, options);
    }

    /// <summary>Applies a sampled tone curve, contrast, gamut handling, and the selected output transfer function.</summary>
    /// <param name="image">Floating scene-linear RGB or RGBA samples in a managed RGB color space.</param>
    /// <param name="options">Tone, contrast, gamut, transfer-function, cancellation, and resource controls.</param>
    /// <returns>A tightly packed floating RGB or RGBA image using the selected transfer function.</returns>
    /// <exception cref="RawFormatException">The image, options, or samples are invalid.</exception>
    /// <exception cref="RawUnsupportedException">The input representation or selected transfer function is unsupported.</exception>
    public static RawRgbImage ApplyToneAndGamut(RawRgbImage image, RawRenderOptions options)
    {
        return RawToneProcessor.Apply(image, options);
    }

    /// <summary>Converts normalized floating RGB or RGBA samples to deterministic eight-bit output with optional ordered dithering.</summary>
    /// <param name="image">Floating RGB or RGBA samples in a managed RGB color space.</param>
    /// <param name="options">Eight-bit selection, dithering, cancellation, and resource controls.</param>
    /// <returns>A tightly packed eight-bit RGB or RGBA image.</returns>
    /// <exception cref="RawFormatException">The image, options, or samples are invalid.</exception>
    /// <exception cref="RawUnsupportedException">The input representation or selected output depth is unsupported.</exception>
    public static RawRgbImage ConvertToEightBit(RawRgbImage image, RawRenderOptions options)
    {
        return RawToneProcessor.ConvertToEightBit(image, options);
    }

    /// <summary>Bakes an optional crop and Exif orientation, resizes with the selected kernel, and converts to deterministic byte, unsigned-short, or floating output.</summary>
    /// <param name="image">Floating gray, RGB, or RGBA samples.</param>
    /// <param name="crop">An optional crop in current image coordinates, or <c>null</c> for the complete image.</param>
    /// <param name="options">Orientation, dimensions, resampler, depth, dithering, cancellation, and resource controls.</param>
    /// <returns>A tightly packed gray, RGB, or RGBA output image.</returns>
    /// <exception cref="RawFormatException">The image, crop, options, samples, or output geometry are invalid.</exception>
    /// <exception cref="RawUnsupportedException">The input representation or requested orientation is unsupported.</exception>
    public static RawRgbImage CreateOutput(RawRgbImage image, RawArea? crop, RawRenderOptions options)
    {
        return RawOutputProcessor.Create(image, crop, options);
    }

    /// <summary>Resolves non-destructive crop precedence without modifying image samples.</summary>
    /// <param name="info">Stored image geometry and camera-defined areas.</param>
    /// <param name="userCrop">An optional caller crop in stored image coordinates.</param>
    /// <returns>The resolved crop hierarchy.</returns>
    /// <exception cref="RawFormatException">A crop is invalid or escapes the active sensor area.</exception>
    public static RawCropModel GetCropModel(RawImageInfo info, RawArea? userCrop)
    {
        return new RawCropModel(info, userCrop);
    }

    internal static RawNormalizationOptions CopyOptions(RawNormalizationOptions options)
    {
        RawNormalizationOptions result = new RawNormalizationOptions();
        result.Limits = options.Limits;
        result.CancellationToken = options.CancellationToken;
        result.UserCrop = options.UserCrop;
        result.LinearizationTable = options.LinearizationTable;
        result.BlackLevel = options.BlackLevel;
        result.WhiteLevels = options.WhiteLevels;
        result.MaskedAreas = options.MaskedAreas;
        result.UseOpticalBlack = options.UseOpticalBlack;
        result.OpticalBlackRepeatWidth = options.OpticalBlackRepeatWidth;
        result.OpticalBlackRepeatHeight = options.OpticalBlackRepeatHeight;
        result.EstimateBlackRowDeltas = options.EstimateBlackRowDeltas;
        result.EstimateBlackColumnDeltas = options.EstimateBlackColumnDeltas;
        result.ApplyRowBandingCorrection = options.ApplyRowBandingCorrection;
        result.ApplyColumnBandingCorrection = options.ApplyColumnBandingCorrection;
        result.MaximumBandingCorrection = options.MaximumBandingCorrection;
        result.ApplyLinearization = options.ApplyLinearization;
        result.SubtractBlackLevel = options.SubtractBlackLevel;
        result.ScaleWhiteLevel = options.ScaleWhiteLevel;
        result.PreserveOriginalValues = options.PreserveOriginalValues;
        return result;
    }
}

namespace qrcode;

internal class ImageDecodeRecovery
{
    static readonly double[] OffsetX = new double[] { -0.25, 0.25, 0, 0, -0.25, 0.25, -0.25, 0.25 };
    static readonly double[] OffsetY = new double[] { 0, 0, -0.25, 0.25, -0.25, -0.25, 0.25, 0.25 };

    // A supplied detection is the starting hypothesis. SampledMatrix is never
    // trusted as image evidence; each trial samples the source image again.
    internal static ImageRecoveryResult RecoverCodewords(QrImage image, QrDetection detection, QrMatrix? expectedPatterns,
        QrDetectionOptions sampling, QrDecodeOptions decoding)
    {
        PerspectiveTransform initial = PerspectiveSamplingPipeline.CreateInitialTransform(detection, expectedPatterns, sampling);
        if (image == null || decoding == null) { throw new QrInputException("Image and decoding options are required.", "image"); }
        if (sampling.MaximumCandidates <= 0 || decoding.MaximumRecoveryAttempts <= 0 || decoding.MaximumSegments <= 0)
        {
            throw new QrInputException("Candidate, attempt, and segment limits must be positive.", "options");
        }

        RecoveryBudget budget = new RecoveryBudget(decoding.MaximumRecoveryAttempts);
        QrPolarity polarity = PerspectiveSamplingPipeline.ResolvePolarity(detection.Polarity, sampling.Polarity);
        PerspectiveSamplingPipeline.ResolvePolarity(polarity, decoding.Polarity);
        if (sampling.BinarizationMode == QrBinarizationMode.SuppliedBinary && image.PixelFormat != QrPixelFormat.Gray8)
        {
            throw new QrInputException("Supplied binary input requires Gray8 pixels.", "PixelFormat");
        }

        int pixels = CheckedDimensions.CalculateArea(image.Width, image.Height, sampling.MaximumImagePixels);
        int grid = CheckedDimensions.CalculateArea(detection.EstimatedWidth, detection.EstimatedHeight, int.MaxValue);
        CheckedDimensions.ValidateDimensionBounds(detection.EstimatedWidth, detection.EstimatedHeight,
            decoding.MaximumMatrixDimension, decoding.MaximumMatrixDimension, int.MaxValue);
        // Luminance, cached binary candidates, preprocessing temporaries, and
        // matrix recovery work are bounded together before image-sized allocation.
        int work = CheckedDimensions.CalculateAllocationBytes(pixels, 12, decoding.MaximumAllocationBytes);
        int gridWork = CheckedDimensions.CalculateAllocationBytes(grid, 320, decoding.MaximumAllocationBytes);
        gridWork = CheckedDimensions.CalculateSum(gridWork, 4096, decoding.MaximumAllocationBytes, "MaximumAllocationBytes");
        CheckedDimensions.CalculateSum(work, gridWork, decoding.MaximumAllocationBytes, "MaximumAllocationBytes");
        QrImage luminance = sampling.BinarizationMode == QrBinarizationMode.SuppliedBinary
            ? LuminanceConverter.ConvertToGray8(image, sampling) : new QrBinarizer().CreateLuminanceImage(image, sampling);
        QrDecodeOptions matrixOptions = CopyDecodeOptions(decoding, sampling, polarity);
        ImageRecoveryResult result = new ImageRecoveryResult();
        int thresholdCount = decoding.EnableRecovery && sampling.BinarizationMode != QrBinarizationMode.SuppliedBinary ? 4 : 1;
        QrRaster?[] rasters = new QrRaster[thresholdCount];
        QrMatrix? patterns = expectedPatterns;
        if (patterns != null) { GridRefiner.ValidateEvidence(patterns, sampling); }
        if (patterns == null && sampling.RefineGrid) { patterns = ImageRecoveryGeometry.CreatePatterns(detection); }
        int strategies = decoding.EnableRecovery ? 4 : 1;
        // Try all thresholds at the original geometry before spending the budget
        // on offsets, independently observed alignment anchors, and local refinement.
        for (int strategy = 0; strategy < strategies; strategy++)
        {
            if (strategy >= 2 && !sampling.RefineGrid) { continue; }
            for (int threshold = 0; threshold < thresholdCount; threshold++)
            {
                if (budget.IsExhausted())
                {
                    result.FailureReason = QrFailureReason.ResourceLimit;
                    return result;
                }
                QrRaster? raster = rasters[threshold];
                if (raster == null)
                {
                    raster = CreateThresholdCandidate(luminance, polarity, sampling, threshold);
                    rasters[threshold] = raster;
                }

                result.ThresholdCandidateIndex = threshold;
                if (strategy == 0)
                {
                    if (TryTransform(raster, initial, null, detection, polarity, sampling, matrixOptions, result, budget)) { return result; }
                }
                else if (strategy == 1)
                {
                    for (int offset = 0; offset < OffsetX.Length; offset++)
                    {
                        result.SamplingOffsetX = OffsetX[offset];
                        result.SamplingOffsetY = OffsetY[offset];
                        if (TryTransform(raster, ImageRecoveryGeometry.Shift(initial, OffsetX[offset], OffsetY[offset]), null,
                            detection, polarity, sampling, matrixOptions, result, budget)) { return result; }
                    }
                    result.SamplingOffsetX = 0;
                    result.SamplingOffsetY = 0;
                }
                else if (strategy == 2 && sampling.RefineGrid)
                {
                    PerspectiveTransform[] alternatives = ImageRecoveryGeometry.FindAlignmentAlternatives(raster, initial, detection, sampling);
                    for (int alignment = 0; alignment < alternatives.Length; alignment++)
                    {
                        result.AlignmentCandidateIndex = alignment;
                        if (TryTransform(raster, alternatives[alignment], null, detection, polarity, sampling, matrixOptions, result, budget)) { return result; }
                        if (patterns != null && TryTransform(raster, alternatives[alignment], patterns,
                            detection, polarity, sampling, matrixOptions, result, budget)) { return result; }
                    }
                    result.AlignmentCandidateIndex = -1;
                }
                else if (strategy == 3 && sampling.RefineGrid && patterns != null)
                {
                    if (TryTransform(raster, initial, patterns, detection, polarity, sampling, matrixOptions, result, budget)) { return result; }
                }
            }
        }

        return result;
    }

    internal static QrDecodeResult Decode(QrImage image, QrDetection detection, QrMatrix? expectedPatterns,
        QrDetectionOptions sampling, QrDecodeOptions decoding)
    {
        ImageRecoveryResult recovered = RecoverCodewords(image, detection, expectedPatterns, sampling, decoding);
        MatrixRecoveryResult? matrix = recovered.Recovery;
        if (!recovered.Success || matrix == null)
        {
            throw new QrDecodeException("Image recovery did not yield verified codewords.", recovered.FailureReason,
                detection.CandidateIndex, recovered.AttemptCount);
        }

        QrDecodeResult result = MatrixDecodeRecovery.CreateDecodedResult(matrix, decoding);
        return AddDiagnostics(result, recovered);
    }

    internal static QrDecodeResult AddDiagnostics(QrDecodeResult result, ImageRecoveryResult recovered)
    {
        result.Detection = recovered.Detection;
        global::System.Collections.Generic.List<string> warnings = new global::System.Collections.Generic.List<string>(result.Warnings);
        if (recovered.ThresholdCandidateIndex != 0) { warnings.Add($"Image recovery used threshold candidate {recovered.ThresholdCandidateIndex}."); }
        if (recovered.SamplingOffsetX != 0 || recovered.SamplingOffsetY != 0) { warnings.Add("Image recovery shifted module sampling positions."); }
        if (recovered.AlignmentCandidateIndex >= 0) { warnings.Add("Image recovery used an independently observed alignment anchor."); }
        if (recovered.UsedRefinement) { warnings.Add("Image recovery refined the sampling homography."); }
        result.Warnings = warnings.ToArray();
        return result;
    }

    static bool TryTransform(QrRaster raster, PerspectiveTransform transform, QrMatrix? patterns, QrDetection detection,
        QrPolarity polarity, QrDetectionOptions sampling, QrDecodeOptions matrixOptions, ImageRecoveryResult result, RecoveryBudget budget)
    {
        if (budget.IsExhausted())
        {
            result.FailureReason = QrFailureReason.ResourceLimit;
            return true;
        }

        QrMatrix sampled;
        bool refined = false;
        if (patterns == null)
        {
            sampled = GridSampler.Sample(raster, transform, detection.EstimatedWidth, detection.EstimatedHeight, sampling);
        }
        else
        {
            GridRefinementResult refinement = GridRefiner.Refine(raster, transform, patterns, sampling);
            transform = refinement.Transform;
            sampled = refinement.Matrix;
            refined = refinement.Improved;
        }

        MatrixRecoveryResult recovery = MatrixDecodeRecovery.RecoverWithinBudget(sampled, matrixOptions, budget);
        result.SampleCount++;
        // A sample with no recognizable matrix family still consumes one trial.
        if (recovery.AttemptCount == 0) { budget.TryBeginAttempt(); }
        result.AttemptCount = budget.AttemptCount;
        if (recovery.Success)
        {
            result.Success = true;
            result.FailureReason = QrFailureReason.None;
            result.Recovery = recovery;
            result.UsedRefinement = refined;
            result.Detection = PerspectiveSamplingPipeline.CreateDetection(raster, detection, transform, sampled, polarity, sampling);
            return true;
        }

        if (recovery.FailureReason == QrFailureReason.ResourceLimit)
        {
            result.FailureReason = QrFailureReason.ResourceLimit;
            return true;
        }
        result.FailureReason = RecoveryFailure.Choose(result.FailureReason, recovery.FailureReason);
        return false;
    }

    static QrRaster CreateThresholdCandidate(QrImage luminance, QrPolarity polarity, QrDetectionOptions options, int candidate)
    {
        return BinaryMorphology.Clean(CreateThresholdRaster(luminance, polarity, options, candidate), options);
    }

    internal static QrRaster CreateThresholdRaster(QrImage luminance, QrPolarity polarity, QrDetectionOptions options, int candidate)
    {
        QrBinarizationMode mode = options.BinarizationMode;
        if (candidate == 1) { mode = mode == QrBinarizationMode.GlobalHistogram ? QrBinarizationMode.HybridLocal : QrBinarizationMode.GlobalHistogram; }
        QrRaster raster;
        if (mode == QrBinarizationMode.SuppliedBinary)
        {
            raster = new QrRaster { Width = luminance.Width, Height = luminance.Height, Stride = luminance.Stride,
                Pixels = (byte[])luminance.Pixels.Clone() };
            BinaryMorphology.ValidateRaster(raster, options);
            if (polarity == QrPolarity.LightOnDark)
            {
                for (int index = 0; index < raster.Pixels.Length; index++) { raster.Pixels[index] = (byte)(255 - raster.Pixels[index]); }
            }
        }
        else if (candidate < 2 && mode != QrBinarizationMode.GlobalHistogram)
        {
            HybridBinarizerResult hybrid = HybridBinarizer.Binarize(luminance, options);
            raster = polarity == QrPolarity.LightOnDark ? hybrid.LightOnDark : hybrid.DarkOnLight;
        }
        else
        {
            GlobalHistogramResult global = GlobalHistogramBinarizer.Binarize(luminance, options);
            raster = polarity == QrPolarity.LightOnDark ? global.LightOnDark : global.DarkOnLight;
            if (candidate >= 2)
            {
                int threshold = global::System.Math.Clamp(global.Threshold + (candidate == 2 ? -16 : 16), 0, 255);
                for (int row = 0; row < raster.Height; row++)
                {
                    for (int column = 0; column < raster.Width; column++)
                    {
                        bool dark = luminance.Pixels[row * luminance.Stride + column] <= threshold;
                        if (polarity == QrPolarity.LightOnDark) { dark = !dark; }
                        raster.Pixels[row * raster.Stride + column] = dark ? (byte)0 : (byte)255;
                    }
                }
            }
        }
        return raster;
    }

    static QrDecodeOptions CopyDecodeOptions(QrDecodeOptions source, QrDetectionOptions sampling, QrPolarity polarity)
    {
        // The raster is already polarity-normalized. Never invert it a second
        // time because the source detection describes light-on-dark printing.
        return new QrDecodeOptions
        {
            AllowModel2 = source.AllowModel2 && sampling.AllowModel2, AllowMicro = source.AllowMicro && sampling.AllowMicro,
            AllowRectangularMicro = source.AllowRectangularMicro && sampling.AllowRectangularMicro,
            AllowLegacyModel1 = source.AllowLegacyModel1 && sampling.AllowLegacyModel1 && polarity == QrPolarity.DarkOnLight,
            Strict = source.Strict, Polarity = QrPolarity.DarkOnLight, TryInverted = false, TryMirrored = source.TryMirrored,
            EnableRecovery = source.EnableRecovery, PreserveUnsupportedEciBytes = source.PreserveUnsupportedEciBytes,
            EnableByteEncodingInference = source.EnableByteEncodingInference, RequireExplicitEci = source.RequireExplicitEci,
            EnableHanziCompatibility = source.EnableHanziCompatibility, MaximumMatrixDimension = source.MaximumMatrixDimension,
            MaximumSegments = source.MaximumSegments, MaximumRecoveryAttempts = source.MaximumRecoveryAttempts,
            MaximumAllocationBytes = source.MaximumAllocationBytes
        };
    }
}

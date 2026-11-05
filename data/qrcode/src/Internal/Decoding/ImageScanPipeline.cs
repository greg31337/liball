using System.Collections.Generic;

namespace qrcode;

internal class ImageScanPipeline
{
    // At most four detection passes, each with MaximumCandidates candidates.
    // Each decode owns one shared MaximumRecoveryAttempts budget. A failed
    // candidate cannot spend another candidate's budget or suppress its result.
    internal static QrDecodeSet ScanImage(QrImage image, QrDetectionOptions detection,
        QrDecodeOptions decoding, bool retainFailures)
    {
        ValidateInput(image, detection, decoding, false);
        QrDetectionOptions options = CreateDetectionOptions(detection, decoding);
        QrImage luminance = new QrBinarizer().CreateLuminanceImage(image, options);
        return ScanPreparedImage(image, luminance, options, decoding, retainFailures, detection.DetectMultiple);
    }

    // Matrix entries represent image pixels, not an already sampled symbol.
    // Only light/dark values are evidence; module metadata is deliberately ignored.
    internal static QrDecodeSet ScanBinaryMatrix(QrMatrix matrix, QrDetectionOptions detection,
        QrDecodeOptions decoding, bool retainFailures)
    {
        if (matrix == null) { throw new QrInputException("The binary matrix is required.", "matrix"); }
        QrImage image = new QrImage
        {
            Width = matrix.Width, Height = matrix.Height, Stride = matrix.Width, PixelFormat = QrPixelFormat.Gray8
        };
        // Check decode work limits before allocating the image-sized conversion.
        ValidateInput(image, detection, decoding, true);
        image.Pixels = DetectorPipeline.CreateBinaryRaster(matrix, detection).Pixels;
        QrDetectionOptions options = CreateDetectionOptions(detection, decoding);
        options.BinarizationMode = QrBinarizationMode.SuppliedBinary;
        options.NormalizeContrast = false;
        return ScanPreparedImage(image, image, options, decoding, retainFailures, detection.DetectMultiple);
    }

    static QrDecodeSet ScanPreparedImage(QrImage image, QrImage luminance, QrDetectionOptions options,
        QrDecodeOptions decoding, bool retainFailures, bool detectMultiple)
    {
        QrDecodeSet result = new QrDecodeSet();
        List<QrDetection> successful = new List<QrDetection>();
        Dictionary<QrDetection, QrDecodeResult> decoded = new Dictionary<QrDetection, QrDecodeResult>();
        List<QrAttemptResult> failures = new List<QrAttemptResult>();
        List<string> diagnostics = new List<string>();
        bool suppliedBinary = options.BinarizationMode == QrBinarizationMode.SuppliedBinary;
        int passes = decoding.EnableRecovery && !suppliedBinary ? 4 : 1;
        int limit = detectMultiple ? options.MaximumCandidates : 1;
        QrDecoder decoder = new QrDecoder();
        for (int pass = 0; pass < passes && successful.Count < limit; pass++)
        {
            QrRaster dark = ImageDecodeRecovery.CreateThresholdRaster(luminance, QrPolarity.DarkOnLight, options, pass);
            QrRaster light = DetectorPipeline.Invert(dark);
            QrDetectionSet candidates = DetectorPipeline.DetectPasses(dark, light, new int[0], options.BinarizationMode, options);
            diagnostics.Add(suppliedBinary ? $"Supplied binary: {candidates.Detections.Length} detections."
                : $"Threshold candidate {pass}: {candidates.Detections.Length} detections.");
            foreach (QrDetection candidate in candidates.Detections)
            {
                candidate.CandidateIndex = result.CandidateCount;
                result.CandidateCount++;
                if (ContainsSymbol(successful, candidate)) { continue; }
                QrAttemptResult attempt;
                long work = (long)image.Width * image.Height * 12
                    + (long)candidate.EstimatedWidth * candidate.EstimatedHeight * 320 + 4096;
                if (candidate.EstimatedWidth > decoding.MaximumMatrixDimension
                    || candidate.EstimatedHeight > decoding.MaximumMatrixDimension || work > decoding.MaximumAllocationBytes)
                {
                    attempt = new QrAttemptResult { FailureReason = QrFailureReason.ResourceLimit, CandidateCount = 1 };
                }
                else
                {
                    attempt = decoder.TryDecodeDetection(image, candidate, options, decoding);
                }
                QrDecodeResult? value = attempt.Result;
                if (!attempt.Success || value == null || value.Detection == null)
                {
                    if (retainFailures)
                    {
                        failures.Add(attempt);
                        diagnostics.Add($"Candidate {candidate.CandidateIndex}: {attempt.FailureReason} after {attempt.AttemptCount} attempts.");
                    }
                    continue;
                }
                // Recovery can refine the geometry. Deduplicate the verified
                // result too, retaining equal payloads at distinct locations.
                if (ContainsSymbol(successful, value.Detection)) { continue; }
                successful.Add(value.Detection);
                decoded.Add(value.Detection, value);
                if (successful.Count == limit) { break; }
            }
        }
        QrDetection[] ordered = DetectionDeduplicator.SelectDetections(successful.ToArray(), options);
        result.Results = new QrDecodeResult[ordered.Length];
        for (int index = 0; index < ordered.Length; index++) { result.Results[index] = decoded[ordered[index]]; }
        result.FailedAttempts = failures.ToArray();
        AssembleSequences(result, options.MaximumCandidates, decoding.MaximumAllocationBytes, diagnostics);
        result.Diagnostics = diagnostics.ToArray();
        return result;
    }

    static bool ContainsSymbol(List<QrDetection> successful, QrDetection candidate)
    {
        foreach (QrDetection previous in successful)
        {
            if (DetectionDeduplicator.RepresentSameSymbol(candidate, previous)) { return true; }
        }
        return false;
    }

    internal static void AssembleSequences(QrDecodeSet result, int maximumResults, int maximumAllocationBytes,
        List<string> diagnostics)
    {
        StructuredAppendGroupingResult grouped = StructuredAppendGrouper.Group(result.Results, maximumResults, maximumAllocationBytes);
        List<QrDecodeResult> assembled = new List<QrDecodeResult>();
        HashSet<QrDecodeResult> unassembled = new HashSet<QrDecodeResult>(grouped.RejectedResults);
        if (grouped.RejectedResults.Length > 0) { diagnostics.Add("Structured append contains incompatible metadata."); }
        foreach (StructuredAppendGroup group in grouped.Groups)
        {
            StructuredAppendAssemblyResult attempt = StructuredAppendAssembly.Assemble(group.Parts, maximumResults, maximumAllocationBytes);
            if (attempt.Success && attempt.Result != null) { assembled.Add(attempt.Result); }
            else
            {
                foreach (QrDecodeResult part in group.Parts) { unassembled.Add(part); }
                diagnostics.Add($"Structured append parity {group.Parity}, count {group.Total}: {attempt.FailureReason}; missing {attempt.MissingIndices.Length} parts.");
            }
        }
        List<QrDecodeResult> ordered = new List<QrDecodeResult>();
        foreach (QrDecodeResult part in result.Results)
        {
            if (unassembled.Contains(part)) { ordered.Add(part); }
        }
        result.AssembledResults = assembled.ToArray();
        result.UnassembledResults = ordered.ToArray();
    }

    static void ValidateInput(QrImage image, QrDetectionOptions detection, QrDecodeOptions decoding, bool suppliedBinary)
    {
        DetectorPipeline.ValidateOptions(detection);
        if (image == null || decoding == null) { throw new QrInputException("Image and decode options are required.", "image"); }
        if (!suppliedBinary && detection.BinarizationMode == QrBinarizationMode.SuppliedBinary)
        {
            throw new QrInputException("Raw-image scanning requires a luminance threshold strategy.", "BinarizationMode");
        }
        if (decoding.MaximumMatrixDimension <= 0 || decoding.MaximumSegments <= 0
            || decoding.MaximumRecoveryAttempts <= 0 || decoding.MaximumAllocationBytes <= 0
            || (int)decoding.Polarity < (int)QrPolarity.Automatic || (int)decoding.Polarity > (int)QrPolarity.LightOnDark)
        {
            throw new QrInputException("Decode limits and polarity must be valid.", "options");
        }
        if (detection.Polarity != QrPolarity.Automatic || decoding.Polarity != QrPolarity.Automatic)
        {
            PerspectiveSamplingPipeline.ResolvePolarity(detection.Polarity, decoding.Polarity);
        }
        CheckedDimensions.ValidateDimensionBounds(image.Width, image.Height,
            detection.MaximumImageWidth, detection.MaximumImageHeight, detection.MaximumImagePixels);
        int pixels = CheckedDimensions.CalculateArea(image.Width, image.Height, detection.MaximumImagePixels);
        int work = CheckedDimensions.CalculateAllocationBytes(pixels, 12, decoding.MaximumAllocationBytes);
        CheckedDimensions.CalculateSum(work, 4096, decoding.MaximumAllocationBytes, "MaximumAllocationBytes");
        // Bound candidate indexing and retained diagnostic storage before scans.
        CheckedDimensions.CalculateAllocationBytes(detection.MaximumCandidates, 4 * 128, decoding.MaximumAllocationBytes);
    }

    static QrDetectionOptions CreateDetectionOptions(QrDetectionOptions source, QrDecodeOptions decoding)
    {
        return new QrDetectionOptions
        {
            AllowModel2 = source.AllowModel2 && decoding.AllowModel2,
            AllowMicro = source.AllowMicro && decoding.AllowMicro,
            AllowRectangularMicro = source.AllowRectangularMicro && decoding.AllowRectangularMicro,
            AllowLegacyModel1 = source.AllowLegacyModel1 && decoding.AllowLegacyModel1,
            BinarizationMode = source.BinarizationMode,
            Polarity = source.Polarity == QrPolarity.Automatic ? decoding.Polarity : source.Polarity,
            EnableDenoise = source.EnableDenoise, EnableGapClosing = source.EnableGapClosing,
            NormalizeContrast = source.NormalizeContrast, AllowCroppedQuietZone = source.AllowCroppedQuietZone,
            // A single-result scan must search past undecodable detections.
            DetectMultiple = true, UseMultiPointSampling = source.UseMultiPointSampling, RefineGrid = source.RefineGrid,
            MinimumModuleSize = source.MinimumModuleSize, MaximumCandidates = source.MaximumCandidates,
            MaximumImageWidth = source.MaximumImageWidth, MaximumImageHeight = source.MaximumImageHeight,
            MaximumImagePixels = source.MaximumImagePixels, MaximumPyramidLevels = source.MaximumPyramidLevels
        };
    }
}

namespace barcode;

internal static class DiagnosticCodes
{
    internal const string PreprocessOriginal = "PREPROCESS_ORIGINAL";
    internal const string PreprocessSmoothed = "PREPROCESS_SMOOTHED";
    internal const string PreprocessSharpened = "PREPROCESS_SHARPENED";
    internal const string PreprocessNormalized = "PREPROCESS_NORMALIZED";
    internal const string BinarizationThreshold = "BINARIZATION_THRESHOLD";
    internal const string CandidateConfidence = "CANDIDATE_CONFIDENCE";
    internal const string CandidateCoordinates = "CANDIDATE_COORDINATES";
    internal const string CandidateNotFound = "CANDIDATE_NOT_FOUND";
    internal const string CandidateDuplicate = "CANDIDATE_DUPLICATE";
    internal const string CandidateConflict = "CANDIDATE_CONFLICT";
    internal const string CandidateLimit = "CANDIDATE_LIMIT";
    internal const string ErrorCorrection = "ERROR_CORRECTION";
    internal const string ResultConfidence = "RESULT_CONFIDENCE";
    internal const string ResultCoordinates = "RESULT_COORDINATES";
}

internal class BoundedDiagnosticCollector
{
    internal const int DefaultMaximumDiagnostics = 128;

    internal int MaximumDiagnostics;
    internal int Count;
    internal int DroppedCount;
    internal global::System.Collections.Generic.List<BarcodeDiagnostic> Diagnostics;

    internal BoundedDiagnosticCollector(int maximumDiagnostics)
    {
        if (maximumDiagnostics < 0)
        {
            throw new BarcodeValidationException(
                "Maximum diagnostic count must not be negative.");
        }

        MaximumDiagnostics = maximumDiagnostics == 0
            ? DefaultMaximumDiagnostics
            : maximumDiagnostics;
        Diagnostics = new global::System.Collections.Generic.List<BarcodeDiagnostic>();
    }

    internal void RecordTransform(
        string code,
        string message,
        BarcodeRectangle? region)
    {
        BarcodeDiagnostic diagnostic = CreateDiagnostic(
            "Preprocessing",
            code,
            message,
            region);
        StoreDiagnostic(diagnostic);
    }

    internal void RecordThreshold(
        double value,
        BarcodeRectangle? region)
    {
        BarcodeDiagnostic diagnostic = CreateNumericDiagnostic(
            "Binarization",
            DiagnosticCodes.BinarizationThreshold,
            "The image stage selected a grayscale binarization threshold.",
            value,
            0.0,
            region);
        StoreDiagnostic(diagnostic);
    }

    internal void RecordCandidateRejection(
        string code,
        string message,
        double count,
        BarcodeRectangle? region)
    {
        BarcodeDiagnostic diagnostic = CreateNumericDiagnostic(
            "Localization",
            code,
            message,
            count,
            0.0,
            region);
        StoreDiagnostic(diagnostic);
    }

    internal void RecordConfidence(
        string stage,
        string code,
        string message,
        double confidence,
        double threshold,
        BarcodeQuadrilateral? location)
    {
        BarcodeRectangle? region = location == null
            ? null
            : GeometryMath.CalculateQuadrilateralBounds(location);
        BarcodeDiagnostic diagnostic = CreateNumericDiagnostic(
            stage,
            code,
            message,
            confidence,
            threshold,
            region);
        StoreDiagnostic(diagnostic);
    }

    internal void RecordCoordinates(
        string stage,
        string code,
        string message,
        BarcodeQuadrilateral? quadrilateral)
    {
        if (quadrilateral == null)
        {
            return;
        }

        BarcodeRectangle region = GeometryMath.CalculateQuadrilateralBounds(quadrilateral);
        BarcodeDiagnostic diagnostic = CreateDiagnostic(
            stage,
            code,
            message,
            region);
        BarcodePoint point = new BarcodePoint();
        point.X = region.X + (region.Width * 0.5);
        point.Y = region.Y + (region.Height * 0.5);
        diagnostic.Location = point;
        StoreDiagnostic(diagnostic);
    }

    internal void RecordDecodeResult(BarcodeDecodeResult result)
    {
        if (result == null)
        {
            throw new BarcodeValidationException(
                "A decode result is required for diagnostics.");
        }

        BarcodeDecodeMetadata? metadata = result.Metadata;
        if (metadata != null &&
            (metadata.ErrorCorrectionApplied ||
             metadata.CorrectedErrorCount > 0 ||
             metadata.ErasureCount > 0))
        {
            double correctionCount = (double)metadata.CorrectedErrorCount +
                metadata.ErasureCount;
            BarcodeRectangle? correctionRegion = result.Location == null
                ? null
                : GeometryMath.CalculateQuadrilateralBounds(result.Location);
            BarcodeDiagnostic correction = CreateNumericDiagnostic(
                "Decode",
                DiagnosticCodes.ErrorCorrection,
                "The decoder applied error correction or erasure recovery.",
                correctionCount,
                0.0,
                correctionRegion);
            StoreDiagnostic(correction);
        }

        if (metadata != null)
        {
            RecordConfidence(
                "Decode",
                DiagnosticCodes.ResultConfidence,
                "The decoder estimated confidence for the recovered result.",
                metadata.Confidence,
                0.0,
                result.Location);
        }

        RecordCoordinates(
            "Decode",
            DiagnosticCodes.ResultCoordinates,
            "The decoder mapped the result to source-image coordinates.",
            result.Location);
    }

    internal void RecordExistingDiagnostic(BarcodeDiagnostic diagnostic)
    {
        if (diagnostic == null)
        {
            throw new BarcodeValidationException(
                "An existing diagnostic is required.");
        }

        StoreDiagnostic(diagnostic);
    }

    internal BarcodeDiagnostic[] CopyDiagnostics()
    {
        BarcodeDiagnostic[] copy = new BarcodeDiagnostic[Diagnostics.Count];
        int index = 0;
        while (index < Diagnostics.Count)
        {
            copy[index] = ResultCopier.CopyDiagnostic(Diagnostics[index]);
            index = index + 1;
        }

        return copy;
    }

    static BarcodeDiagnostic CreateDiagnostic(
        string stage,
        string code,
        string message,
        BarcodeRectangle? region)
    {
        if (stage == null || code == null || message == null)
        {
            throw new BarcodeValidationException(
                "Diagnostic stage, code, and message are required.");
        }

        BarcodeDiagnostic diagnostic = new BarcodeDiagnostic();
        diagnostic.Stage = stage;
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.Region = ResultCopier.CopyRectangle(region);
        return diagnostic;
    }

    static BarcodeDiagnostic CreateNumericDiagnostic(
        string stage,
        string code,
        string message,
        double value,
        double threshold,
        BarcodeRectangle? region)
    {
        GeometryMath.ValidateCoordinate(value);
        GeometryMath.ValidateCoordinate(threshold);
        BarcodeDiagnostic diagnostic = CreateDiagnostic(
            stage,
            code,
            message,
            region);
        diagnostic.HasNumericEvidence = true;
        diagnostic.Value = value;
        diagnostic.Threshold = threshold;
        return diagnostic;
    }

    void StoreDiagnostic(BarcodeDiagnostic diagnostic)
    {
        if (Count >= MaximumDiagnostics)
        {
            DroppedCount = DroppedCount + 1;
            return;
        }

        Diagnostics.Add(ResultCopier.CopyDiagnostic(diagnostic));
        Count = Count + 1;
    }
}

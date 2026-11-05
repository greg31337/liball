namespace barcode;

internal static class BarcodeDetectionPipeline
{
    internal static BarcodeDetectionResult Detect(BarcodeDetectionRequest request)
    {
        if (request == null || request.Image == null)
        {
            throw new BarcodeDetectionException(
                "A barcode detection request with an image is required.");
        }

        BarcodeDetectionOptions options = request.Options == null
            ? CreateDefaultOptions()
            : request.Options;
        ManagedGrayImage gray = ImageIngestion.ConvertToGray8(request.Image);
        ImageCropResult crop = ImageCropper.CropToRegion(
            gray,
            options.RegionOfInterest);
        ImageFallbackResult localized = ImageFallbackPipeline.Execute(
            crop.Image,
            options.AllowedFormats,
            options.Effort,
            options.BinarizationMode,
            options.LocalizationMode,
            options.Polarity,
            options.ExpectedCandidateCount,
            options.MaximumCandidates,
            options.MaximumDiagnostics);
        BarcodeCandidate[] candidates = new BarcodeCandidate[localized.Candidates.Length];
        int index = 0;
        while (index < localized.Candidates.Length)
        {
            BarcodeCandidate candidate = ResultCopier.CopyCandidate(
                localized.Candidates[index].Score.Candidate);
            if (candidate.Location != null)
            {
                candidate.Location = crop.Transform.RestoreQuadrilateral(
                    candidate.Location);
            }

            candidate.Diagnostics = RestoreDiagnostics(
                candidate.Diagnostics,
                crop.Transform);
            candidates[index] = candidate;
            index = index + 1;
        }

        BarcodeDetectionResult result = new BarcodeDetectionResult();
        result.Candidates = candidates;
        result.Diagnostics = RestoreDiagnostics(
            localized.Diagnostics,
            crop.Transform);
        return result;
    }

    internal static BarcodeDiagnostic[] RestoreDiagnostics(
        BarcodeDiagnostic[]? diagnostics,
        ImageCoordinateTransform transform)
    {
        if (diagnostics == null)
        {
            return new BarcodeDiagnostic[0];
        }

        BarcodeDiagnostic[] restored = new BarcodeDiagnostic[diagnostics.Length];
        int index = 0;
        while (index < diagnostics.Length)
        {
            BarcodeDiagnostic diagnostic = ResultCopier.CopyDiagnostic(
                diagnostics[index]);
            if (diagnostic.Location != null)
            {
                diagnostic.Location = transform.RestorePoint(
                    diagnostic.Location);
            }

            if (diagnostic.Region != null)
            {
                diagnostic.Region = transform.RestoreRectangle(
                    diagnostic.Region);
            }

            restored[index] = diagnostic;
            index = index + 1;
        }

        return restored;
    }

    static BarcodeDetectionOptions CreateDefaultOptions()
    {
        BarcodeDetectionOptions options = new BarcodeDetectionOptions();
        options.Effort = BarcodeEffort.Balanced;
        options.BinarizationMode = BarcodeBinarizationMode.Automatic;
        options.LocalizationMode = BarcodeLocalizationMode.Automatic;
        options.Polarity = BarcodePolarity.Automatic;
        options.MirrorMode = BarcodeMirrorMode.Both;
        return options;
    }
}

namespace barcode;

internal class ImageBarcodeDecodeResult
{
    internal BarcodeDecodeResult[] Results = new BarcodeDecodeResult[0];
    internal BarcodeDiagnostic[] Diagnostics = new BarcodeDiagnostic[0];
}

internal static class ImageBarcodeDecoder
{
    internal static ImageBarcodeDecodeResult Decode(
        BarcodeImage image,
        BarcodeDecodeOptions options)
    {
        ManagedGrayImage gray = ImageIngestion.ConvertToGray8(image);
        ImageCropResult crop = ImageCropper.CropToRegion(
            gray,
            options.RegionOfInterest);
        int candidateLimit = options.MaximumResults == 0
            ? ImageFallbackPipeline.DefaultMaximumCandidates
            : options.MaximumResults;
        ImageFallbackResult localized = ImageFallbackPipeline.Execute(
            crop.Image,
            options.AllowedFormats,
            options.Effort,
            options.BinarizationMode,
            options.LocalizationMode,
            options.Polarity,
            0,
            candidateLimit,
            options.MaximumDiagnostics);
        global::System.Collections.Generic.List<BarcodeDecodeResult> decodedResults =
            new global::System.Collections.Generic.List<BarcodeDecodeResult>();
        int index = 0;
        while (index < localized.Candidates.Length)
        {
            BarcodeDecodeResult? decoded = DecodeCandidate(
                crop.Image,
                localized,
                localized.Candidates[index],
                options);
            if (decoded != null && IsAllowedLength(decoded, options.AllowedLengths))
            {
                if (decoded.Location != null)
                {
                    decoded.Location = crop.Transform.RestoreQuadrilateral(
                        decoded.Location);
                }

                decodedResults.Add(decoded);
            }

            index = index + 1;
        }

        BarcodeDecodeResult[] decodedArray =
            new BarcodeDecodeResult[decodedResults.Count];
        index = 0;
        while (index < decodedResults.Count)
        {
            decodedArray[index] = decodedResults[index];
            index = index + 1;
        }

        MultiSymbolAggregationResult aggregation =
            MultiSymbolResultAggregator.Combine(
                decodedArray,
                options.MaximumResults,
                options.MaximumDiagnostics);
        BoundedDiagnosticCollector diagnostics =
            new BoundedDiagnosticCollector(options.MaximumDiagnostics);
        BarcodeDiagnostic[] localizationDiagnostics =
            BarcodeDetectionPipeline.RestoreDiagnostics(
                localized.Diagnostics,
                crop.Transform);
        AddDiagnostics(diagnostics, localizationDiagnostics);
        AddDiagnostics(diagnostics, aggregation.Diagnostics);
        ImageBarcodeDecodeResult result = new ImageBarcodeDecodeResult();
        result.Results = aggregation.Results;
        result.Diagnostics = diagnostics.CopyDiagnostics();
        return result;
    }

    static BarcodeDecodeResult? DecodeCandidate(
        ManagedGrayImage image,
        ImageFallbackResult localized,
        ImagePipelineCandidate candidate,
        BarcodeDecodeOptions options)
    {
        if (candidate.StageIndex < 0 ||
            candidate.StageIndex >= localized.Stages.Length)
        {
            throw new BarcodeValidationException(
                "Image candidate stage index is invalid.");
        }

        ImagePipelineStageResult stageResult = localized.Stages[candidate.StageIndex];
        ManagedGrayImage prepared = ImageFallbackPipeline.PrepareImage(
            image,
            stageResult.Stage.Preprocessing);
        ImageBinarizationResult binary = ImageBinarizer.Binarize(
            prepared,
            stageResult.Stage.Binarization,
            stageResult.ActualPolarity);
        BarcodeDecodeOptions normalizedOptions = CopyNormalizedOptions(options);
        BarcodeDecodeResult? decoded;
        if (candidate.Localization.DecodeResult != null)
        {
            decoded = ResultCopier.CopyDecodeResult(
                candidate.Localization.DecodeResult);
        }
        else if (candidate.Localization.Kind == CommonCandidateKind.DataMatrix)
        {
            decoded = DecodeDataMatrixCandidate(
                binary.Modules,
                candidate.Localization.DataMatrixEvidence,
                normalizedOptions);
        }
        else if (candidate.Localization.Kind == CommonCandidateKind.Pdf417)
        {
            decoded = DecodePdf417Candidate(
                binary.Modules,
                candidate.Localization.Pdf417Evidence,
                normalizedOptions);
        }
        else
        {
            decoded = DecodeLinearCandidate(
                binary.Modules,
                candidate.Localization.Candidate,
                normalizedOptions);
        }

        if (decoded != null)
        {
            decoded.Polarity = stageResult.ActualPolarity;
        }

        return decoded;
    }

    static BarcodeDecodeResult? DecodeDataMatrixCandidate(
        ModuleMatrix image,
        DataMatrixFinderEvidence? evidence,
        BarcodeDecodeOptions options)
    {
        if (evidence == null)
        {
            return null;
        }

        DataMatrixImageDecodeResult? decoded =
            DataMatrixProjectiveDecoder.TryDecodeCandidate(
                image,
                evidence,
                options);
        return decoded == null ? null : decoded.Decode.Result;
    }

    static BarcodeDecodeResult? DecodePdf417Candidate(
        ModuleMatrix image,
        Pdf417FinderEvidence? evidence,
        BarcodeDecodeOptions options)
    {
        if (evidence == null)
        {
            return null;
        }

        Pdf417ImageDecodeResult? decoded =
            Pdf417ProjectiveDecoder.TryDecodeCandidate(
                image,
                evidence,
                options);
        return decoded == null ? null : decoded.Decode.Result;
    }

    static BarcodeDecodeResult? DecodeLinearCandidate(
        ModuleMatrix image,
        BarcodeCandidate candidate,
        BarcodeDecodeOptions options)
    {
        if (candidate.Location == null)
        {
            return null;
        }

        if (candidate.Format == BarcodeFormat.EanUpcAddon2 ||
            candidate.Format == BarcodeFormat.EanUpcAddon5)
        {
            return DecodeAddOnCandidate(image, candidate, options);
        }

        BarcodeQuadrilateral scanLocation = ExpandLinearLocation(
            candidate.Location,
            image);
        BarcodeRectangle bounds = GeometryMath.CalculateQuadrilateralBounds(
            scanLocation);
        double spacing = global::System.Math.Max(1.0, bounds.Height / 8.0);
        AngledScanlineConsensus? consensus = AngledLinearScanner.Scan(
            image,
            candidate.Format,
            scanLocation,
            spacing,
            5,
            1,
            false);
        return consensus == null ? null : consensus.Result;
    }

    static BarcodeQuadrilateral ExpandLinearLocation(
        BarcodeQuadrilateral location,
        ModuleMatrix image)
    {
        BarcodePoint topLeft = GeometryMath.RequirePoint(location.TopLeft);
        BarcodePoint topRight = GeometryMath.RequirePoint(location.TopRight);
        BarcodePoint bottomRight = GeometryMath.RequirePoint(location.BottomRight);
        BarcodePoint bottomLeft = GeometryMath.RequirePoint(location.BottomLeft);
        double topLength = GeometryMath.DistanceBetweenPoints(topLeft, topRight);
        double bottomLength = GeometryMath.DistanceBetweenPoints(
            bottomLeft,
            bottomRight);
        double topPadding = global::System.Math.Max(4.0, topLength * 0.40);
        double bottomPadding = global::System.Math.Max(4.0, bottomLength * 0.40);
        BarcodePoint expandedTopLeft = ExpandPoint(
            topLeft,
            topRight,
            -topPadding,
            topLength,
            image);
        BarcodePoint expandedTopRight = ExpandPoint(
            topLeft,
            topRight,
            topLength + topPadding,
            topLength,
            image);
        BarcodePoint expandedBottomRight = ExpandPoint(
            bottomLeft,
            bottomRight,
            bottomLength + bottomPadding,
            bottomLength,
            image);
        BarcodePoint expandedBottomLeft = ExpandPoint(
            bottomLeft,
            bottomRight,
            -bottomPadding,
            bottomLength,
            image);
        return GeometryMath.CreateQuadrilateral(
            expandedTopLeft,
            expandedTopRight,
            expandedBottomRight,
            expandedBottomLeft);
    }

    static BarcodePoint ExpandPoint(
        BarcodePoint start,
        BarcodePoint end,
        double distance,
        double length,
        ModuleMatrix image)
    {
        double ratio = length <= GeometryMath.Epsilon ? 0.0 : distance / length;
        double x = start.X + ((end.X - start.X) * ratio);
        double y = start.Y + ((end.Y - start.Y) * ratio);
        x = global::System.Math.Max(0.0, global::System.Math.Min(image.Width - 1.0, x));
        y = global::System.Math.Max(0.0, global::System.Math.Min(image.Height - 1.0, y));
        return GeometryMath.CreatePoint(x, y);
    }

    static BarcodeDecodeResult? DecodeAddOnCandidate(
        ModuleMatrix image,
        BarcodeCandidate candidate,
        BarcodeDecodeOptions options)
    {
        BarcodeQuadrilateral location = candidate.Location == null
            ? new BarcodeQuadrilateral()
            : candidate.Location;
        AngledScanlineSample[] samples = DeskewedScanlineFan.Create(
            image,
            location,
            1.0,
            5,
            false);
        EanUpcOptions eanOptions = new EanUpcOptions();
        eanOptions.AllowedAddOnLengths = ResultCopier.CopyIntArray(
            options.AllowedLengths);
        int index = 0;
        while (index < samples.Length)
        {
            EanUpcAddonDecodedData? decoded =
                EanUpcAddonScanlineDecoder.TryDecodeRow(
                    samples[index].Row,
                    eanOptions);
            if (decoded != null && decoded.Format == candidate.Format)
            {
                BarcodeDecodeResult result = decoded.Result;
                result.Location = ResultCopier.CopyQuadrilateral(location);
                return result;
            }

            index = index + 1;
        }

        return null;
    }

    static BarcodeDecodeOptions CopyNormalizedOptions(BarcodeDecodeOptions source)
    {
        BarcodeDecodeOptions copy = new BarcodeDecodeOptions();
        copy.AllowedFormats = CopyFormats(source.AllowedFormats);
        copy.AllowedLengths = ResultCopier.CopyIntArray(source.AllowedLengths);
        copy.MaximumResults = source.MaximumResults;
        copy.ExpectedResultCount = source.ExpectedResultCount;
        copy.PureSymbol = source.PureSymbol;
        copy.ChecksumMode = source.ChecksumMode;
        copy.ParseGs1 = source.ParseGs1;
        copy.CharacterEncoding = source.CharacterEncoding;
        copy.Polarity = BarcodePolarity.DarkOnLight;
        copy.MirrorMode = source.MirrorMode;
        copy.Rotations = CopyRotations(source.Rotations);
        copy.Effort = source.Effort;
        copy.BinarizationMode = source.BinarizationMode;
        copy.LocalizationMode = source.LocalizationMode;
        copy.MaximumDiagnostics = source.MaximumDiagnostics;
        return copy;
    }

    static BarcodeFormat[]? CopyFormats(BarcodeFormat[]? source)
    {
        if (source == null)
        {
            return null;
        }

        BarcodeFormat[] copy = new BarcodeFormat[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    static BarcodeRotation[]? CopyRotations(BarcodeRotation[]? source)
    {
        if (source == null)
        {
            return null;
        }

        BarcodeRotation[] copy = new BarcodeRotation[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    static bool IsAllowedLength(
        BarcodeDecodeResult result,
        int[]? allowedLengths)
    {
        if (allowedLengths == null || allowedLengths.Length == 0)
        {
            return true;
        }

        int length = result.Payload == null || result.Payload.RawBytes == null
            ? 0
            : result.Payload.RawBytes.Length;
        int index = 0;
        while (index < allowedLengths.Length)
        {
            if (allowedLengths[index] == length)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static void AddDiagnostics(
        BoundedDiagnosticCollector collector,
        BarcodeDiagnostic[] diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Length)
        {
            collector.RecordExistingDiagnostic(diagnostics[index]);
            index = index + 1;
        }
    }
}

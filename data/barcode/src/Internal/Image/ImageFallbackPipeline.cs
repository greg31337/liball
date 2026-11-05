namespace barcode;

internal enum ImagePreprocessingKind
{
    Original = 0,
    Smoothed = 1,
    Sharpened = 2,
    Normalized = 3
}

internal class ImagePipelineStage
{
    internal int Index;
    internal ImagePreprocessingKind Preprocessing;
    internal BarcodeBinarizationMode Binarization;
    internal BarcodeLocalizationMode Localization;
}

internal class ImagePipelineStageResult
{
    internal ImagePipelineStage Stage = new ImagePipelineStage();
    internal BarcodeBinarizationMode ActualBinarization;
    internal BarcodePolarity ActualPolarity;
    internal int LocatedCount;
    internal int AcceptedCount;
    internal bool StoppedAfterStage;
    internal CandidateScore[] Scores = new CandidateScore[0];
}

internal class ImagePipelineCandidate
{
    internal CommonLocalizationCandidate Localization = new CommonLocalizationCandidate();
    internal CandidateScore Score = new CandidateScore();
    internal int StageIndex;
}

internal class ImageFallbackResult
{
    internal ImagePipelineCandidate[] Candidates = new ImagePipelineCandidate[0];
    internal ImagePipelineStageResult[] Stages = new ImagePipelineStageResult[0];
    internal int StagesExecuted;
    internal int DuplicateCount;
    internal int ConflictCount;
    internal int DroppedDiagnosticCount;
    internal BarcodeDiagnostic[] Diagnostics = new BarcodeDiagnostic[0];
    internal bool ExpectedCountReached;
    internal bool MaximumCountReached;
}

internal static class ImageEffortSchedule
{
    internal static ImagePipelineStage[] Create(
        BarcodeEffort effort,
        BarcodeBinarizationMode requestedBinarization,
        BarcodeLocalizationMode requestedLocalization)
    {
        ValidateEffort(effort);
        ValidateBinarization(requestedBinarization);
        ValidateLocalization(requestedLocalization);
        global::System.Collections.Generic.List<ImagePipelineStage> stages =
            new global::System.Collections.Generic.List<ImagePipelineStage>();
        if (requestedLocalization == BarcodeLocalizationMode.Automatic)
        {
            AddAutomaticStages(stages, effort, requestedBinarization);
        }
        else
        {
            AddExplicitStages(
                stages,
                effort,
                requestedBinarization,
                requestedLocalization);
        }

        int index = 0;
        while (index < stages.Count)
        {
            stages[index].Index = index;
            index = index + 1;
        }

        return stages.ToArray();
    }

    static void AddAutomaticStages(
        global::System.Collections.Generic.List<ImagePipelineStage> stages,
        BarcodeEffort effort,
        BarcodeBinarizationMode requestedBinarization)
    {
        AddStage(
            stages,
            ImagePreprocessingKind.Original,
            ResolveBinarization(requestedBinarization, BarcodeBinarizationMode.Otsu),
            BarcodeLocalizationMode.Direct);
        AddStage(
            stages,
            ImagePreprocessingKind.Original,
            ResolveBinarization(requestedBinarization, BarcodeBinarizationMode.Otsu),
            BarcodeLocalizationMode.ConnectedBlocks);
        AddStage(
            stages,
            ImagePreprocessingKind.Original,
            ResolveBinarization(requestedBinarization, BarcodeBinarizationMode.Otsu),
            BarcodeLocalizationMode.FinderPatterns);
        if (effort == BarcodeEffort.Fast)
        {
            return;
        }

        AddStage(
            stages,
            ImagePreprocessingKind.Smoothed,
            ResolveBinarization(requestedBinarization, BarcodeBinarizationMode.Hybrid),
            BarcodeLocalizationMode.Automatic);
        if (effort == BarcodeEffort.Balanced)
        {
            return;
        }

        AddStage(
            stages,
            ImagePreprocessingKind.Smoothed,
            ResolveBinarization(requestedBinarization, BarcodeBinarizationMode.Row),
            BarcodeLocalizationMode.ConnectedBlocks);
        AddStage(
            stages,
            ImagePreprocessingKind.Sharpened,
            ResolveBinarization(requestedBinarization, BarcodeBinarizationMode.Adaptive),
            BarcodeLocalizationMode.FinderPatterns);
        AddStage(
            stages,
            ImagePreprocessingKind.Normalized,
            ResolveBinarization(requestedBinarization, BarcodeBinarizationMode.Adaptive),
            BarcodeLocalizationMode.ConnectedBlocks);
        AddStage(
            stages,
            ImagePreprocessingKind.Normalized,
            ResolveBinarization(requestedBinarization, BarcodeBinarizationMode.Hybrid),
            BarcodeLocalizationMode.FinderPatterns);
    }

    static void AddExplicitStages(
        global::System.Collections.Generic.List<ImagePipelineStage> stages,
        BarcodeEffort effort,
        BarcodeBinarizationMode requestedBinarization,
        BarcodeLocalizationMode localization)
    {
        AddStage(
            stages,
            ImagePreprocessingKind.Original,
            ResolveBinarization(requestedBinarization, BarcodeBinarizationMode.Otsu),
            localization);
        if (effort == BarcodeEffort.Fast)
        {
            return;
        }

        AddStage(
            stages,
            ImagePreprocessingKind.Smoothed,
            ResolveBinarization(requestedBinarization, BarcodeBinarizationMode.Hybrid),
            localization);
        if (effort == BarcodeEffort.Balanced)
        {
            return;
        }

        AddStage(
            stages,
            ImagePreprocessingKind.Sharpened,
            ResolveBinarization(requestedBinarization, BarcodeBinarizationMode.Adaptive),
            localization);
        AddStage(
            stages,
            ImagePreprocessingKind.Normalized,
            ResolveBinarization(requestedBinarization, BarcodeBinarizationMode.Hybrid),
            localization);
    }

    static void AddStage(
        global::System.Collections.Generic.List<ImagePipelineStage> stages,
        ImagePreprocessingKind preprocessing,
        BarcodeBinarizationMode binarization,
        BarcodeLocalizationMode localization)
    {
        ImagePipelineStage stage = new ImagePipelineStage();
        stage.Preprocessing = preprocessing;
        stage.Binarization = binarization;
        stage.Localization = localization;
        stages.Add(stage);
    }

    static BarcodeBinarizationMode ResolveBinarization(
        BarcodeBinarizationMode requested,
        BarcodeBinarizationMode automaticChoice)
    {
        return requested == BarcodeBinarizationMode.Automatic
            ? automaticChoice
            : requested;
    }

    static void ValidateEffort(BarcodeEffort effort)
    {
        if (effort != BarcodeEffort.Fast &&
            effort != BarcodeEffort.Balanced &&
            effort != BarcodeEffort.Thorough)
        {
            throw new BarcodeValidationException("Image pipeline effort is invalid.");
        }
    }

    static void ValidateBinarization(BarcodeBinarizationMode mode)
    {
        if (mode != BarcodeBinarizationMode.Global &&
            mode != BarcodeBinarizationMode.Otsu &&
            mode != BarcodeBinarizationMode.Row &&
            mode != BarcodeBinarizationMode.Adaptive &&
            mode != BarcodeBinarizationMode.Hybrid &&
            mode != BarcodeBinarizationMode.Automatic)
        {
            throw new BarcodeValidationException("Image pipeline binarization mode is invalid.");
        }
    }

    static void ValidateLocalization(BarcodeLocalizationMode mode)
    {
        if (mode != BarcodeLocalizationMode.Direct &&
            mode != BarcodeLocalizationMode.ConnectedBlocks &&
            mode != BarcodeLocalizationMode.Lines &&
            mode != BarcodeLocalizationMode.FinderPatterns &&
            mode != BarcodeLocalizationMode.Automatic)
        {
            throw new BarcodeValidationException("Image pipeline localization mode is invalid.");
        }
    }
}

internal static class DirectCandidateLocator
{
    internal static CommonLocalizationCandidate[] Locate(
        ModuleMatrix image,
        BarcodeFormat[]? allowedFormats,
        BarcodePolarity polarity)
    {
        if (image == null)
        {
            throw new BarcodeValidationException("Direct candidate image must not be null.");
        }

        BarcodeFormat format = ResolveDirectFormat(allowedFormats);
        if (format == (BarcodeFormat)(-1))
        {
            return new CommonLocalizationCandidate[0];
        }

        global::System.Collections.Generic.List<CommonLocalizationCandidate> candidates =
            new global::System.Collections.Generic.List<CommonLocalizationCandidate>();
        int horizontalSpacing = global::System.Math.Max(1, image.Height / 16);
        LinearScanlineConsensus? horizontal = LinearDirectScanner.Scan(
            image,
            format,
            false,
            (image.Height - 1) / 2,
            horizontalSpacing,
            5,
            1);
        if (horizontal != null)
        {
            candidates.Add(CreateCandidate(horizontal, polarity, 0));
        }

        int verticalSpacing = global::System.Math.Max(1, image.Width / 16);
        LinearScanlineConsensus? vertical = LinearDirectScanner.Scan(
            image,
            format,
            true,
            (image.Width - 1) / 2,
            verticalSpacing,
            5,
            1);
        if (vertical != null)
        {
            candidates.Add(CreateCandidate(vertical, polarity, candidates.Count));
        }

        return candidates.ToArray();
    }

    static BarcodeFormat ResolveDirectFormat(BarcodeFormat[]? allowedFormats)
    {
        if (allowedFormats == null || allowedFormats.Length == 0)
        {
            return BarcodeFormat.Automatic;
        }

        BarcodeFormat selected = BarcodeFormat.Automatic;
        int count = 0;
        int index = 0;
        while (index < allowedFormats.Length)
        {
            BarcodeFormat format = allowedFormats[index];
            if (format == BarcodeFormat.Automatic)
            {
                return BarcodeFormat.Automatic;
            }

            if (IsDirectFormat(format))
            {
                selected = format;
                count = count + 1;
            }

            index = index + 1;
        }

        if (count == 0)
        {
            return (BarcodeFormat)(-1);
        }

        return count == 1 ? selected : BarcodeFormat.Automatic;
    }

    static bool IsDirectFormat(BarcodeFormat format)
    {
        return format == BarcodeFormat.Code128 || format == BarcodeFormat.Gs1128 ||
            format == BarcodeFormat.Ean8 || format == BarcodeFormat.Ean13 ||
            format == BarcodeFormat.UpcA || format == BarcodeFormat.UpcE ||
            format == BarcodeFormat.EanUpcAddon2 ||
            format == BarcodeFormat.EanUpcAddon5 ||
            format == BarcodeFormat.Code39 || format == BarcodeFormat.Itf ||
            format == BarcodeFormat.Itf14 || format == BarcodeFormat.Codabar;
    }

    static CommonLocalizationCandidate CreateCandidate(
        LinearScanlineConsensus consensus,
        BarcodePolarity polarity,
        int sourceIndex)
    {
        BarcodeCandidate candidate = new BarcodeCandidate();
        candidate.Format = consensus.Result.Format;
        candidate.Dimension = BarcodeDimension.Linear;
        candidate.Location = ResultCopier.CopyQuadrilateral(consensus.Result.Location);
        candidate.AngleDegrees = consensus.Result.OrientationDegrees;
        candidate.Confidence = consensus.Result.Metadata == null
            ? (double)consensus.VoteCount / consensus.ObservationCount
            : consensus.Result.Metadata.Confidence;
        candidate.Polarity = polarity;
        candidate.MirrorMode = consensus.Result.MirrorMode;
        candidate.Diagnostics = new BarcodeDiagnostic[0];
        CommonLocalizationCandidate common = new CommonLocalizationCandidate();
        common.Candidate = candidate;
        common.Kind = CommonCandidateKind.Linear;
        common.Family = FormatDispatcher.ResolveCodecFamily(candidate.Format);
        common.SourceIndex = sourceIndex;
        common.DecodeResult = ResultCopier.CopyDecodeResult(consensus.Result);
        return common;
    }
}

internal static class ImageFallbackPipeline
{
    internal const int DefaultMaximumCandidates = 64;

    internal static ImageFallbackResult Execute(
        ManagedGrayImage image,
        BarcodeFormat[]? allowedFormats,
        BarcodeEffort effort,
        BarcodeBinarizationMode binarization,
        BarcodeLocalizationMode localization,
        BarcodePolarity polarity,
        int expectedCount,
        int maximumCount,
        int maximumDiagnostics)
    {
        HistogramAnalyzer.ValidateGrayImage(image);
        ValidateCounts(expectedCount, maximumCount);
        BoundedDiagnosticCollector diagnostics =
            new BoundedDiagnosticCollector(maximumDiagnostics);
        BarcodeRectangle imageRegion = GeometryMath.CreateRectangle(
            0.0,
            0.0,
            image.Width,
            image.Height);
        int limit = maximumCount == 0
            ? DefaultMaximumCandidates
            : maximumCount;
        ImagePipelineStage[] schedule = ImageEffortSchedule.Create(
            effort,
            binarization,
            localization);
        global::System.Collections.Generic.List<ImagePipelineCandidate> accepted =
            new global::System.Collections.Generic.List<ImagePipelineCandidate>();
        global::System.Collections.Generic.List<ImagePipelineStageResult> stageResults =
            new global::System.Collections.Generic.List<ImagePipelineStageResult>();
        bool expectedReached = false;
        bool maximumReached = false;
        int duplicateCount = 0;
        int conflictCount = 0;
        int stageIndex = 0;
        while (stageIndex < schedule.Length && !expectedReached && !maximumReached)
        {
            ImagePipelineStage stage = schedule[stageIndex];
            diagnostics.RecordTransform(
                ResolvePreprocessingCode(stage.Preprocessing),
                ResolvePreprocessingMessage(stage.Preprocessing),
                imageRegion);
            ManagedGrayImage prepared = PrepareImage(image, stage.Preprocessing);
            ImageBinarizationResult binary = ImageBinarizer.Binarize(
                prepared,
                stage.Binarization,
                polarity);
            diagnostics.RecordThreshold(binary.GlobalThreshold, imageRegion);
            CommonLocalizationCandidate[] located = LocateStage(
                binary.Modules,
                allowedFormats,
                stage.Localization);
            located = FilterCandidatesWithinImage(located, binary.Modules);
            CandidateScore[] scores = CandidateScorer.ScoreAndRank(
                binary.Modules,
                prepared,
                located,
                null);
            int scoreIndex = 0;
            while (scoreIndex < scores.Length)
            {
                scores[scoreIndex].Source.Candidate.Polarity = binary.Polarity;
                scores[scoreIndex].Candidate.Polarity = binary.Polarity;
                diagnostics.RecordConfidence(
                    "Localization",
                    DiagnosticCodes.CandidateConfidence,
                    "The localization pipeline scored a barcode candidate.",
                    scores[scoreIndex].Total,
                    0.0,
                    scores[scoreIndex].Candidate.Location);
                diagnostics.RecordCoordinates(
                    "Localization",
                    DiagnosticCodes.CandidateCoordinates,
                    "The localization pipeline mapped a candidate to image coordinates.",
                    scores[scoreIndex].Candidate.Location);
                scoreIndex = scoreIndex + 1;
            }

            int previousCount = accepted.Count;
            global::System.Collections.Generic.List<ImagePipelineCandidate> proposed =
                new global::System.Collections.Generic.List<ImagePipelineCandidate>();
            int index = 0;
            while (index < accepted.Count)
            {
                proposed.Add(accepted[index]);
                index = index + 1;
            }

            index = 0;
            while (index < scores.Length)
            {
                ImagePipelineCandidate candidate = new ImagePipelineCandidate();
                candidate.Localization = scores[index].Source;
                candidate.Score = scores[index];
                candidate.StageIndex = stageIndex;
                proposed.Add(candidate);
                index = index + 1;
            }

            ImagePipelineCandidate[] proposedCandidates =
                new ImagePipelineCandidate[proposed.Count];
            index = 0;
            while (index < proposed.Count)
            {
                proposedCandidates[index] = proposed[index];
                index = index + 1;
            }

            ImageCandidateAggregationResult aggregation =
                ImageCandidateAggregator.Combine(proposedCandidates, limit);
            accepted.Clear();
            index = 0;
            while (index < aggregation.Candidates.Length)
            {
                accepted.Add(aggregation.Candidates[index]);
                index = index + 1;
            }

            int take = global::System.Math.Max(0, accepted.Count - previousCount);
            duplicateCount = duplicateCount + aggregation.DuplicateCount;
            conflictCount = conflictCount + aggregation.ConflictCount;
            RecordCandidateRejections(
                diagnostics,
                aggregation,
                scores.Length,
                imageRegion);
            expectedReached = expectedCount > 0 && accepted.Count >= expectedCount;
            maximumReached = aggregation.MaximumReached;
            ImagePipelineStageResult stageResult = new ImagePipelineStageResult();
            stageResult.Stage = stage;
            stageResult.ActualBinarization = binary.Mode;
            stageResult.ActualPolarity = binary.Polarity;
            stageResult.LocatedCount = located.Length;
            stageResult.AcceptedCount = take;
            stageResult.StoppedAfterStage = expectedReached || maximumReached;
            stageResult.Scores = scores;
            stageResults.Add(stageResult);
            stageIndex = stageIndex + 1;
        }

        ImageFallbackResult result = new ImageFallbackResult();
        result.Candidates = accepted.ToArray();
        result.Stages = stageResults.ToArray();
        result.StagesExecuted = stageResults.Count;
        result.DuplicateCount = duplicateCount;
        result.ConflictCount = conflictCount;
        result.Diagnostics = diagnostics.CopyDiagnostics();
        result.DroppedDiagnosticCount = diagnostics.DroppedCount;
        result.ExpectedCountReached = expectedReached;
        result.MaximumCountReached = maximumReached;
        return result;
    }

    static void RecordCandidateRejections(
        BoundedDiagnosticCollector diagnostics,
        ImageCandidateAggregationResult aggregation,
        int locatedCount,
        BarcodeRectangle imageRegion)
    {
        if (locatedCount == 0)
        {
            diagnostics.RecordCandidateRejection(
                DiagnosticCodes.CandidateNotFound,
                "The localization stage did not produce a barcode candidate.",
                1.0,
                imageRegion);
        }

        if (aggregation.DuplicateCount > 0)
        {
            diagnostics.RecordCandidateRejection(
                DiagnosticCodes.CandidateDuplicate,
                "Repeated observations of the same physical candidate were suppressed.",
                aggregation.DuplicateCount,
                imageRegion);
        }

        if (aggregation.ConflictCount > 0)
        {
            diagnostics.RecordCandidateRejection(
                DiagnosticCodes.CandidateConflict,
                "Conflicting format observations at matching geometry were resolved.",
                aggregation.ConflictCount,
                imageRegion);
        }

        int limitedCount = aggregation.UniqueCount - aggregation.Candidates.Length;
        if (limitedCount > 0)
        {
            diagnostics.RecordCandidateRejection(
                DiagnosticCodes.CandidateLimit,
                "Candidates beyond the configured result limit were rejected.",
                limitedCount,
                imageRegion);
        }
    }

    static string ResolvePreprocessingCode(ImagePreprocessingKind preprocessing)
    {
        if (preprocessing == ImagePreprocessingKind.Original)
        {
            return DiagnosticCodes.PreprocessOriginal;
        }

        if (preprocessing == ImagePreprocessingKind.Smoothed)
        {
            return DiagnosticCodes.PreprocessSmoothed;
        }

        if (preprocessing == ImagePreprocessingKind.Sharpened)
        {
            return DiagnosticCodes.PreprocessSharpened;
        }

        if (preprocessing == ImagePreprocessingKind.Normalized)
        {
            return DiagnosticCodes.PreprocessNormalized;
        }

        throw new BarcodeValidationException("Image preprocessing kind is invalid.");
    }

    static string ResolvePreprocessingMessage(ImagePreprocessingKind preprocessing)
    {
        if (preprocessing == ImagePreprocessingKind.Original)
        {
            return "The image stage used the original grayscale samples.";
        }

        if (preprocessing == ImagePreprocessingKind.Smoothed)
        {
            return "The image stage applied box smoothing.";
        }

        if (preprocessing == ImagePreprocessingKind.Sharpened)
        {
            return "The image stage applied unsharp enhancement.";
        }

        if (preprocessing == ImagePreprocessingKind.Normalized)
        {
            return "The image stage normalized uneven illumination.";
        }

        throw new BarcodeValidationException("Image preprocessing kind is invalid.");
    }

    internal static ManagedGrayImage PrepareImage(
        ManagedGrayImage source,
        ImagePreprocessingKind preprocessing)
    {
        if (preprocessing == ImagePreprocessingKind.Original)
        {
            return ImageFilter.CopyGray(source);
        }

        if (preprocessing == ImagePreprocessingKind.Smoothed)
        {
            return ImageFilter.BoxSmooth(source, 1);
        }

        if (preprocessing == ImagePreprocessingKind.Sharpened)
        {
            return ImageFilter.UnsharpEnhance(source, 1, 1.0);
        }

        if (preprocessing == ImagePreprocessingKind.Normalized)
        {
            int minimumDimension = global::System.Math.Min(source.Width, source.Height);
            int radius = minimumDimension / 8;
            if (radius < 1)
            {
                radius = 1;
            }
            else if (radius > ImageNormalizer.MaximumNormalizationRadius)
            {
                radius = ImageNormalizer.MaximumNormalizationRadius;
            }

            return ImageNormalizer.NormalizeIllumination(source, radius, 4);
        }

        throw new BarcodeValidationException("Image preprocessing kind is invalid.");
    }

    static CommonLocalizationCandidate[] LocateStage(
        ModuleMatrix image,
        BarcodeFormat[]? allowedFormats,
        BarcodeLocalizationMode localization)
    {
        if (localization == BarcodeLocalizationMode.Direct)
        {
            return DirectCandidateLocator.Locate(
                image,
                allowedFormats,
                BarcodePolarity.DarkOnLight);
        }

        BarcodeFormat[]? selectedFormats = FilterFormats(
            allowedFormats,
            localization);
        if (selectedFormats != null && selectedFormats.Length == 0)
        {
            return new CommonLocalizationCandidate[0];
        }

        return CommonCandidatePipeline.Locate(
            image,
            selectedFormats,
            BarcodePolarity.DarkOnLight,
            3,
            12);
    }

    static CommonLocalizationCandidate[] FilterCandidatesWithinImage(
        CommonLocalizationCandidate[] candidates,
        ModuleMatrix image)
    {
        global::System.Collections.Generic.List<CommonLocalizationCandidate> accepted =
            new global::System.Collections.Generic.List<CommonLocalizationCandidate>();
        int index = 0;
        while (index < candidates.Length)
        {
            BarcodeQuadrilateral? location = candidates[index].Candidate.Location;
            if (location != null)
            {
                BarcodeRectangle bounds =
                    GeometryMath.CalculateQuadrilateralBounds(location);
                if (bounds.X >= -1.0 && bounds.Y >= -1.0 &&
                    bounds.X + bounds.Width <= image.Width + 1.0 &&
                    bounds.Y + bounds.Height <= image.Height + 1.0)
                {
                    accepted.Add(candidates[index]);
                }
            }

            index = index + 1;
        }

        return accepted.ToArray();
    }

    static BarcodeFormat[]? FilterFormats(
        BarcodeFormat[]? allowedFormats,
        BarcodeLocalizationMode localization)
    {
        if (localization == BarcodeLocalizationMode.Automatic)
        {
            return allowedFormats;
        }

        BarcodeFormat[] source = allowedFormats == null || allowedFormats.Length == 0
            ? FormatDispatcher.CreateSupportedFormats()
            : allowedFormats;
        global::System.Collections.Generic.List<BarcodeFormat> selected =
            new global::System.Collections.Generic.List<BarcodeFormat>();
        int index = 0;
        while (index < source.Length)
        {
            BarcodeFormat format = source[index];
            if (format == BarcodeFormat.Automatic)
            {
                source = FormatDispatcher.CreateSupportedFormats();
                selected.Clear();
                index = 0;
                continue;
            }

            BarcodeDimension dimension = FormatDispatcher.ResolveDimension(format);
            bool include = localization == BarcodeLocalizationMode.FinderPatterns
                ? dimension != BarcodeDimension.Linear
                : dimension == BarcodeDimension.Linear;
            if (include && !ContainsFormat(selected, format))
            {
                selected.Add(format);
            }

            index = index + 1;
        }

        return selected.ToArray();
    }

    static bool ContainsFormat(
        global::System.Collections.Generic.List<BarcodeFormat> formats,
        BarcodeFormat format)
    {
        int index = 0;
        while (index < formats.Count)
        {
            if (formats[index] == format)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static void ValidateCounts(int expectedCount, int maximumCount)
    {
        if (expectedCount < 0 || maximumCount < 0)
        {
            throw new BarcodeValidationException("Image pipeline result counts must not be negative.");
        }
    }
}

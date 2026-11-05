namespace barcode;

internal enum CommonCandidateKind
{
    Linear = 0,
    DataMatrix = 1,
    Pdf417 = 2
}

internal class CommonLocalizationCandidate
{
    internal BarcodeCandidate Candidate = new BarcodeCandidate();
    internal CommonCandidateKind Kind;
    internal BarcodeCodecFamily Family;
    internal int SourceIndex;
    internal BarcodeDecodeResult? DecodeResult;
    internal LinearBarcodeCandidate? LinearEvidence;
    internal DataMatrixFinderEvidence? DataMatrixEvidence;
    internal Pdf417FinderEvidence? Pdf417Evidence;
}

internal static class CommonCandidatePipeline
{
    internal static CommonLocalizationCandidate[] Locate(
        ModuleMatrix image,
        BarcodeFormat[]? allowedFormats,
        BarcodePolarity polarity,
        int minimumLinearBarCount,
        int maximumLinearGap)
    {
        if (image == null)
        {
            throw new BarcodeValidationException("Candidate pipeline image must not be null.");
        }

        ValidatePolarity(polarity);
        if (minimumLinearBarCount < 2)
        {
            throw new BarcodeValidationException("Candidate pipeline minimum linear bar count must be at least two.");
        }

        if (maximumLinearGap < 0)
        {
            throw new BarcodeValidationException("Candidate pipeline maximum linear gap must not be negative.");
        }

        BarcodeFormat[] formats = ResolveFormats(allowedFormats);
        global::System.Collections.Generic.List<CommonLocalizationCandidate> candidates =
            new global::System.Collections.Generic.List<CommonLocalizationCandidate>();
        AddLinearCandidates(
            candidates,
            image,
            formats,
            polarity,
            minimumLinearBarCount,
            maximumLinearGap);
        AddDataMatrixCandidates(candidates, image, formats, polarity);
        AddPdf417Candidates(candidates, image, formats, polarity);
        return candidates.ToArray();
    }

    static BarcodeFormat[] ResolveFormats(BarcodeFormat[]? allowedFormats)
    {
        if (allowedFormats == null || allowedFormats.Length == 0)
        {
            return FormatDispatcher.CreateSupportedFormats();
        }

        int index = 0;
        while (index < allowedFormats.Length)
        {
            if (allowedFormats[index] == BarcodeFormat.Automatic)
            {
                return FormatDispatcher.CreateSupportedFormats();
            }

            if (!FormatDispatcher.IsSupportedFormat(allowedFormats[index]))
            {
                throw new BarcodeValidationException("Candidate pipeline format is unsupported.");
            }

            index = index + 1;
        }

        BarcodeFormat[] buffer = new BarcodeFormat[allowedFormats.Length];
        int count = 0;
        index = 0;
        while (index < allowedFormats.Length)
        {
            BarcodeFormat format = allowedFormats[index];
            if (!ContainsFormat(buffer, count, format))
            {
                buffer[count] = format;
                count = count + 1;
            }

            index = index + 1;
        }

        BarcodeFormat[] formats = new BarcodeFormat[count];
        index = 0;
        while (index < count)
        {
            formats[index] = buffer[index];
            index = index + 1;
        }

        return formats;
    }

    static bool ContainsFormat(BarcodeFormat[] formats, int count, BarcodeFormat format)
    {
        int index = 0;
        while (index < count)
        {
            if (formats[index] == format)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static void AddLinearCandidates(
        global::System.Collections.Generic.List<CommonLocalizationCandidate> candidates,
        ModuleMatrix image,
        BarcodeFormat[] formats,
        BarcodePolarity polarity,
        int minimumBarCount,
        int maximumGap)
    {
        BarcodeFormat linearFormat = SelectLinearCandidateFormat(formats);
        if (!HasLinearFormat(formats))
        {
            return;
        }

        if (polarity == BarcodePolarity.DarkOnLight || polarity == BarcodePolarity.Automatic)
        {
            AddLinearPolarityCandidates(
                candidates,
                image,
                linearFormat,
                BarcodePolarity.DarkOnLight,
                minimumBarCount,
                maximumGap);
        }

        if (polarity == BarcodePolarity.LightOnDark || polarity == BarcodePolarity.Automatic)
        {
            ModuleMatrix inverted = InvertMatrix(image);
            AddLinearPolarityCandidates(
                candidates,
                inverted,
                linearFormat,
                BarcodePolarity.LightOnDark,
                minimumBarCount,
                maximumGap);
        }
    }

    static void AddLinearPolarityCandidates(
        global::System.Collections.Generic.List<CommonLocalizationCandidate> candidates,
        ModuleMatrix image,
        BarcodeFormat format,
        BarcodePolarity polarity,
        int minimumBarCount,
        int maximumGap)
    {
        LinearBarcodeCandidate[] found = LinearLocalizer.Locate(
            image,
            minimumBarCount,
            maximumGap);
        int index = 0;
        while (index < found.Length)
        {
            LinearBarcodeCandidate evidence = found[index];
            BarcodeCandidate candidate = new BarcodeCandidate();
            candidate.Format = format;
            candidate.Dimension = BarcodeDimension.Linear;
            candidate.Location = ResultCopier.CopyQuadrilateral(evidence.Location);
            candidate.AngleDegrees = evidence.BarsVertical ? 0.0 : 90.0;
            candidate.Confidence = evidence.OrientationScore;
            candidate.Polarity = polarity;
            candidate.MirrorMode = BarcodeMirrorMode.Normal;
            candidate.Diagnostics = new BarcodeDiagnostic[0];

            CommonLocalizationCandidate common = new CommonLocalizationCandidate();
            common.Candidate = candidate;
            common.Kind = CommonCandidateKind.Linear;
            common.Family = ResolveLinearFamily(format);
            common.SourceIndex = index;
            common.LinearEvidence = evidence;
            candidates.Add(common);
            index = index + 1;
        }
    }

    static BarcodeCodecFamily ResolveLinearFamily(BarcodeFormat format)
    {
        if (format == BarcodeFormat.Automatic)
        {
            return BarcodeCodecFamily.Code128;
        }

        return FormatDispatcher.ResolveCodecFamily(format);
    }

    static bool HasLinearFormat(BarcodeFormat[] formats)
    {
        int index = 0;
        while (index < formats.Length)
        {
            if (FormatDispatcher.ResolveDimension(formats[index]) == BarcodeDimension.Linear)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static BarcodeFormat SelectLinearCandidateFormat(BarcodeFormat[] formats)
    {
        BarcodeFormat selected = BarcodeFormat.Automatic;
        int count = 0;
        int index = 0;
        while (index < formats.Length)
        {
            if (FormatDispatcher.ResolveDimension(formats[index]) == BarcodeDimension.Linear)
            {
                selected = formats[index];
                count = count + 1;
            }

            index = index + 1;
        }

        return count == 1 ? selected : BarcodeFormat.Automatic;
    }

    static ModuleMatrix InvertMatrix(ModuleMatrix image)
    {
        ModuleMatrix inverted = new ModuleMatrix(image.Width, image.Height);
        int y = 0;
        while (y < image.Height)
        {
            int x = 0;
            while (x < image.Width)
            {
                inverted.SetModule(x, y, !image.GetModule(x, y));
                x = x + 1;
            }

            y = y + 1;
        }

        return inverted;
    }

    static void AddDataMatrixCandidates(
        global::System.Collections.Generic.List<CommonLocalizationCandidate> candidates,
        ModuleMatrix image,
        BarcodeFormat[] formats,
        BarcodePolarity polarity)
    {
        bool allowDataMatrix = HasFormat(formats, BarcodeFormat.DataMatrix);
        bool allowGs1DataMatrix = HasFormat(formats, BarcodeFormat.Gs1DataMatrix);
        if (!allowDataMatrix && !allowGs1DataMatrix)
        {
            return;
        }

        BarcodeFormat candidateFormat = allowDataMatrix
            ? BarcodeFormat.DataMatrix
            : BarcodeFormat.Gs1DataMatrix;
        DataMatrixFinderEvidence[] found = DataMatrixImageFinder.Locate(image, polarity);
        int index = 0;
        while (index < found.Length)
        {
            DataMatrixFinderEvidence evidence = found[index];
            BarcodeCandidate candidate = CopyCandidate(evidence.Candidate);
            candidate.Format = candidateFormat;
            CommonLocalizationCandidate common = new CommonLocalizationCandidate();
            common.Candidate = candidate;
            common.Kind = CommonCandidateKind.DataMatrix;
            common.Family = BarcodeCodecFamily.DataMatrix;
            common.SourceIndex = index;
            common.DataMatrixEvidence = evidence;
            candidates.Add(common);
            index = index + 1;
        }
    }

    static void AddPdf417Candidates(
        global::System.Collections.Generic.List<CommonLocalizationCandidate> candidates,
        ModuleMatrix image,
        BarcodeFormat[] formats,
        BarcodePolarity polarity)
    {
        if (image.Width < Pdf417ImageFinder.MinimumImageDimension ||
            image.Height < Pdf417ImageFinder.MinimumImageDimension)
        {
            return;
        }

        bool allowStandard = HasFormat(formats, BarcodeFormat.Pdf417);
        bool allowMacro = HasFormat(formats, BarcodeFormat.MacroPdf417);
        ModuleMatrix working = Pdf417ProjectiveDecoder.CopyProjectiveMatrix(
            image);
        int sourceIndex = 0;
        if (allowStandard || allowMacro)
        {
            bool continueSearching = true;
            while (continueSearching
                && sourceIndex < Pdf417ProjectiveDecoder.DefaultMaximumResultCount)
            {
                Pdf417FinderEvidence? standard = Pdf417ImageFinder.Locate(
                    working,
                    polarity);
                if (standard == null)
                {
                    continueSearching = false;
                }
                else
                {
                    BarcodeFormat format = allowStandard
                        ? BarcodeFormat.Pdf417
                        : BarcodeFormat.MacroPdf417;
                    AddPdf417Candidate(
                        candidates,
                        standard,
                        format,
                        sourceIndex);
                    Pdf417ProjectiveDecoder.MaskFinderRegion(
                        working,
                        standard);
                    sourceIndex = sourceIndex + 1;
                }
            }
        }

        if (HasFormat(formats, BarcodeFormat.CompactPdf417))
        {
            bool continueSearching = true;
            while (continueSearching
                && sourceIndex < Pdf417ProjectiveDecoder.DefaultMaximumResultCount)
            {
                Pdf417FinderEvidence? compact = Pdf417ImageFinder.LocateCompact(
                    working,
                    polarity);
                if (compact == null)
                {
                    continueSearching = false;
                }
                else
                {
                    AddPdf417Candidate(
                        candidates,
                        compact,
                        BarcodeFormat.CompactPdf417,
                        sourceIndex);
                    Pdf417ProjectiveDecoder.MaskFinderRegion(
                        working,
                        compact);
                    sourceIndex = sourceIndex + 1;
                }
            }
        }
    }

    static void AddPdf417Candidate(
        global::System.Collections.Generic.List<CommonLocalizationCandidate> candidates,
        Pdf417FinderEvidence evidence,
        BarcodeFormat format,
        int sourceIndex)
    {
        BarcodeCandidate candidate = CopyCandidate(evidence.Candidate);
        candidate.Format = format;
        CommonLocalizationCandidate common = new CommonLocalizationCandidate();
        common.Candidate = candidate;
        common.Kind = CommonCandidateKind.Pdf417;
        common.Family = BarcodeCodecFamily.Pdf417;
        common.SourceIndex = sourceIndex;
        common.Pdf417Evidence = evidence;
        candidates.Add(common);
    }

    static bool HasFormat(BarcodeFormat[] formats, BarcodeFormat format)
    {
        int index = 0;
        while (index < formats.Length)
        {
            if (formats[index] == format)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static BarcodeCandidate CopyCandidate(BarcodeCandidate source)
    {
        if (source == null)
        {
            throw new BarcodeValidationException("Specialized localization candidate must not be null.");
        }

        BarcodeCandidate copy = new BarcodeCandidate();
        copy.Format = source.Format;
        copy.Dimension = source.Dimension;
        copy.Location = ResultCopier.CopyQuadrilateral(source.Location);
        copy.AngleDegrees = source.AngleDegrees;
        copy.Confidence = source.Confidence;
        copy.Polarity = source.Polarity;
        copy.MirrorMode = source.MirrorMode;
        copy.Diagnostics = ResultCopier.CopyDiagnosticArray(source.Diagnostics);
        return copy;
    }

    static void ValidatePolarity(BarcodePolarity polarity)
    {
        if (polarity != BarcodePolarity.DarkOnLight &&
            polarity != BarcodePolarity.LightOnDark &&
            polarity != BarcodePolarity.Automatic)
        {
            throw new BarcodeValidationException("Candidate pipeline polarity is invalid.");
        }
    }
}

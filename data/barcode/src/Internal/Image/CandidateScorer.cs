namespace barcode;

internal class CandidateDecodeEvidence
{
    internal BarcodeDecodeResult? Result;
    internal int AttemptCount;
    internal int AgreementCount;
}

internal class CandidateScore
{
    internal CommonLocalizationCandidate Source = new CommonLocalizationCandidate();
    internal BarcodeCandidate Candidate = new BarcodeCandidate();
    internal int OriginalIndex;
    internal double FormatEvidence;
    internal double GeometryEvidence;
    internal double QuietZoneEvidence;
    internal double ContrastEvidence;
    internal double IntegrityEvidence;
    internal double AgreementEvidence;
    internal double Total;
}

internal static class CandidateScorer
{
    internal const double FormatWeight = 0.25;
    internal const double GeometryWeight = 0.20;
    internal const double QuietZoneWeight = 0.15;
    internal const double ContrastWeight = 0.15;
    internal const double IntegrityWeight = 0.15;
    internal const double AgreementWeight = 0.10;
    internal const int MaximumQuietZoneSampleWidth = 16;

    internal static CandidateScore[] ScoreAndRank(
        ModuleMatrix binaryImage,
        ManagedGrayImage grayImage,
        CommonLocalizationCandidate[] candidates,
        CandidateDecodeEvidence[]? decodeEvidence)
    {
        ValidateInputs(binaryImage, grayImage, candidates, decodeEvidence);
        CandidateScore[] scores = new CandidateScore[candidates.Length];
        int index = 0;
        while (index < candidates.Length)
        {
            CandidateDecodeEvidence? decode = decodeEvidence == null
                ? null
                : decodeEvidence[index];
            scores[index] = CalculateScore(
                binaryImage,
                grayImage,
                candidates[index],
                decode,
                index);
            index = index + 1;
        }

        SortScores(scores);
        return scores;
    }

    static CandidateScore CalculateScore(
        ModuleMatrix binaryImage,
        ManagedGrayImage grayImage,
        CommonLocalizationCandidate source,
        CandidateDecodeEvidence? decode,
        int originalIndex)
    {
        BarcodeQuadrilateral location = RequireLocation(source.Candidate, binaryImage);
        CandidateScore score = new CandidateScore();
        score.Source = source;
        score.Candidate = CopyCandidate(source.Candidate);
        score.OriginalIndex = originalIndex;
        score.FormatEvidence = CalculateFormatEvidence(source);
        score.GeometryEvidence = CalculateGeometryEvidence(location);
        score.QuietZoneEvidence = CalculateQuietZoneEvidence(
            binaryImage,
            location,
            source.Candidate.Polarity);
        score.ContrastEvidence = CalculateContrastEvidence(grayImage, location);
        score.IntegrityEvidence = CalculateIntegrityEvidence(decode);
        score.AgreementEvidence = CalculateAgreementEvidence(decode);
        score.Total =
            (score.FormatEvidence * FormatWeight) +
            (score.GeometryEvidence * GeometryWeight) +
            (score.QuietZoneEvidence * QuietZoneWeight) +
            (score.ContrastEvidence * ContrastWeight) +
            (score.IntegrityEvidence * IntegrityWeight) +
            (score.AgreementEvidence * AgreementWeight);
        score.Total = ClampScore(score.Total);
        score.Candidate.Confidence = score.Total;
        return score;
    }

    static double CalculateFormatEvidence(CommonLocalizationCandidate source)
    {
        double candidateConfidence = ClampScore(source.Candidate.Confidence);
        if (source.Kind == CommonCandidateKind.Linear && source.LinearEvidence != null)
        {
            return ClampScore(
                (candidateConfidence + source.LinearEvidence.OrientationScore) * 0.5);
        }

        if (source.Kind == CommonCandidateKind.DataMatrix && source.DataMatrixEvidence != null)
        {
            DataMatrixFinderEvidence evidence = source.DataMatrixEvidence;
            return ClampScore(
                (evidence.SolidBorderScore * 0.30) +
                (evidence.ClockTrackScore * 0.30) +
                (evidence.DimensionScore * 0.20) +
                (evidence.Confidence * 0.20));
        }

        if (source.Kind == CommonCandidateKind.Pdf417 && source.Pdf417Evidence != null)
        {
            Pdf417FinderEvidence evidence = source.Pdf417Evidence;
            return ClampScore(
                (evidence.StartLineScore * 0.20) +
                (evidence.StopLineScore * 0.20) +
                (evidence.DimensionScore * 0.20) +
                (evidence.LineCoverage * 0.15) +
                (evidence.Confidence * 0.25));
        }

        return candidateConfidence;
    }

    static double CalculateGeometryEvidence(BarcodeQuadrilateral location)
    {
        BarcodePoint topLeft = GeometryMath.RequirePoint(location.TopLeft);
        BarcodePoint topRight = GeometryMath.RequirePoint(location.TopRight);
        BarcodePoint bottomRight = GeometryMath.RequirePoint(location.BottomRight);
        BarcodePoint bottomLeft = GeometryMath.RequirePoint(location.BottomLeft);
        double top = GeometryMath.DistanceBetweenPoints(topLeft, topRight);
        double bottom = GeometryMath.DistanceBetweenPoints(bottomLeft, bottomRight);
        double left = GeometryMath.DistanceBetweenPoints(topLeft, bottomLeft);
        double right = GeometryMath.DistanceBetweenPoints(topRight, bottomRight);
        if (top <= GeometryMath.Epsilon || bottom <= GeometryMath.Epsilon ||
            left <= GeometryMath.Epsilon || right <= GeometryMath.Epsilon)
        {
            return 0.0;
        }

        double horizontalConsistency = global::System.Math.Min(top, bottom) /
            global::System.Math.Max(top, bottom);
        double verticalConsistency = global::System.Math.Min(left, right) /
            global::System.Math.Max(left, right);
        BarcodeRectangle bounds = GeometryMath.CalculateQuadrilateralBounds(location);
        double boundsArea = bounds.Width * bounds.Height;
        if (boundsArea <= GeometryMath.Epsilon)
        {
            return 0.0;
        }

        double area = global::System.Math.Abs(
            GeometryMath.CalculateQuadrilateralSignedArea(location));
        double coverage = ClampScore(area / boundsArea);
        return ClampScore(
            (horizontalConsistency + verticalConsistency + coverage) / 3.0);
    }

    static double CalculateQuietZoneEvidence(
        ModuleMatrix image,
        BarcodeQuadrilateral location,
        BarcodePolarity polarity)
    {
        BarcodeRectangle bounds = GeometryMath.CalculateQuadrilateralBounds(location);
        int innerLeft = (int)global::System.Math.Floor(bounds.X);
        int innerTop = (int)global::System.Math.Floor(bounds.Y);
        int innerRight = (int)global::System.Math.Ceiling(bounds.X + bounds.Width) - 1;
        int innerBottom = (int)global::System.Math.Ceiling(bounds.Y + bounds.Height) - 1;
        int minimumDimension = (int)global::System.Math.Floor(
            global::System.Math.Min(bounds.Width, bounds.Height));
        int sampleWidth = minimumDimension / 10;
        if (sampleWidth < 1)
        {
            sampleWidth = 1;
        }
        else if (sampleWidth > MaximumQuietZoneSampleWidth)
        {
            sampleWidth = MaximumQuietZoneSampleWidth;
        }

        bool expectedDark = ResolveExpectedBackground(image, polarity);
        int matches = 0;
        int total = 0;
        int y = innerTop - sampleWidth;
        while (y <= innerBottom + sampleWidth)
        {
            int x = innerLeft - sampleWidth;
            while (x <= innerRight + sampleWidth)
            {
                bool inside = x >= innerLeft && x <= innerRight &&
                    y >= innerTop && y <= innerBottom;
                if (!inside)
                {
                    total = total + 1;
                    if (x >= 0 && x < image.Width && y >= 0 && y < image.Height &&
                        image.GetModule(x, y) == expectedDark)
                    {
                        matches = matches + 1;
                    }
                }

                x = x + 1;
            }

            y = y + 1;
        }

        return total == 0 ? 0.0 : (double)matches / total;
    }

    static bool ResolveExpectedBackground(ModuleMatrix image, BarcodePolarity polarity)
    {
        if (polarity == BarcodePolarity.LightOnDark)
        {
            return true;
        }

        if (polarity == BarcodePolarity.DarkOnLight)
        {
            return false;
        }

        int dark = 0;
        int total = 0;
        int x = 0;
        while (x < image.Width)
        {
            if (image.GetModule(x, 0))
            {
                dark = dark + 1;
            }

            total = total + 1;
            if (image.Height > 1)
            {
                if (image.GetModule(x, image.Height - 1))
                {
                    dark = dark + 1;
                }

                total = total + 1;
            }

            x = x + 1;
        }

        int y = 1;
        while (y < image.Height - 1)
        {
            if (image.GetModule(0, y))
            {
                dark = dark + 1;
            }

            total = total + 1;
            if (image.Width > 1)
            {
                if (image.GetModule(image.Width - 1, y))
                {
                    dark = dark + 1;
                }

                total = total + 1;
            }

            y = y + 1;
        }

        return dark * 2 >= total;
    }

    static double CalculateContrastEvidence(
        ManagedGrayImage image,
        BarcodeQuadrilateral location)
    {
        BarcodeRectangle bounds = GeometryMath.CalculateQuadrilateralBounds(location);
        int padding = 1;
        int left = global::System.Math.Max(0, (int)global::System.Math.Floor(bounds.X) - padding);
        int top = global::System.Math.Max(0, (int)global::System.Math.Floor(bounds.Y) - padding);
        int right = global::System.Math.Min(
            image.Width - 1,
            (int)global::System.Math.Ceiling(bounds.X + bounds.Width) + padding);
        int bottom = global::System.Math.Min(
            image.Height - 1,
            (int)global::System.Math.Ceiling(bounds.Y + bounds.Height) + padding);
        int minimum = 255;
        int maximum = 0;
        int y = top;
        while (y <= bottom)
        {
            int x = left;
            while (x <= right)
            {
                int value = image.ReadSample(x, y);
                minimum = global::System.Math.Min(minimum, value);
                maximum = global::System.Math.Max(maximum, value);
                x = x + 1;
            }

            y = y + 1;
        }

        return (double)(maximum - minimum) / 255.0;
    }

    static double CalculateIntegrityEvidence(CandidateDecodeEvidence? decode)
    {
        if (decode == null || decode.Result == null)
        {
            return 0.0;
        }

        BarcodeDecodeResult result = decode.Result;
        BarcodeDecodeMetadata? metadata = result.Metadata;
        if (metadata == null)
        {
            return 0.5;
        }

        if (metadata.HasChecksum)
        {
            return metadata.ChecksumValid ? 1.0 : 0.0;
        }

        if (result.Dimension == BarcodeDimension.Matrix ||
            result.Dimension == BarcodeDimension.StackedLinear)
        {
            int totalCodewords = metadata.Codewords == null
                ? 1
                : global::System.Math.Max(1, metadata.Codewords.Length);
            double correctionFraction =
                (double)(metadata.CorrectedErrorCount + metadata.ErasureCount) /
                totalCodewords;
            return ClampScore(1.0 - correctionFraction);
        }

        return 0.75;
    }

    static double CalculateAgreementEvidence(CandidateDecodeEvidence? decode)
    {
        if (decode == null || decode.AttemptCount == 0)
        {
            return 0.0;
        }

        return (double)decode.AgreementCount / decode.AttemptCount;
    }

    static BarcodeQuadrilateral RequireLocation(
        BarcodeCandidate candidate,
        ModuleMatrix image)
    {
        if (candidate == null || candidate.Location == null)
        {
            throw new BarcodeValidationException("Scored candidate location must not be null.");
        }

        GeometryMath.ValidateQuadrilateral(candidate.Location);
        BarcodeRectangle bounds = GeometryMath.CalculateQuadrilateralBounds(candidate.Location);
        if (bounds.X < -1.0 || bounds.Y < -1.0 ||
            bounds.X + bounds.Width > image.Width + 1.0 ||
            bounds.Y + bounds.Height > image.Height + 1.0)
        {
            throw new BarcodeValidationException("Scored candidate location is outside the image.");
        }

        return candidate.Location;
    }

    static BarcodeCandidate CopyCandidate(BarcodeCandidate source)
    {
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

    static void SortScores(CandidateScore[] scores)
    {
        int index = 1;
        while (index < scores.Length)
        {
            CandidateScore current = scores[index];
            int insertion = index;
            while (insertion > 0 && ComesBefore(current, scores[insertion - 1]))
            {
                scores[insertion] = scores[insertion - 1];
                insertion = insertion - 1;
            }

            scores[insertion] = current;
            index = index + 1;
        }
    }

    static bool ComesBefore(CandidateScore first, CandidateScore second)
    {
        if (first.Total > second.Total)
        {
            return true;
        }

        if (first.Total < second.Total)
        {
            return false;
        }

        if (first.FormatEvidence > second.FormatEvidence)
        {
            return true;
        }

        if (first.FormatEvidence < second.FormatEvidence)
        {
            return false;
        }

        return first.OriginalIndex < second.OriginalIndex;
    }

    static double ClampScore(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0.0)
        {
            return 0.0;
        }

        if (value > 1.0)
        {
            return 1.0;
        }

        return value;
    }

    static void ValidateInputs(
        ModuleMatrix binaryImage,
        ManagedGrayImage grayImage,
        CommonLocalizationCandidate[] candidates,
        CandidateDecodeEvidence[]? decodeEvidence)
    {
        if (binaryImage == null || grayImage == null || candidates == null)
        {
            throw new BarcodeValidationException("Candidate scoring inputs must not be null.");
        }

        HistogramAnalyzer.ValidateGrayImage(grayImage);
        if (binaryImage.Width != grayImage.Width || binaryImage.Height != grayImage.Height)
        {
            throw new BarcodeValidationException("Candidate scoring images must have matching dimensions.");
        }

        if (decodeEvidence != null && decodeEvidence.Length != candidates.Length)
        {
            throw new BarcodeValidationException("Candidate decode evidence count does not match the candidates.");
        }

        int index = 0;
        while (index < candidates.Length)
        {
            if (candidates[index] == null || candidates[index].Candidate == null)
            {
                throw new BarcodeValidationException("Candidate scoring entry is incomplete.");
            }

            if (decodeEvidence != null)
            {
                CandidateDecodeEvidence evidence = decodeEvidence[index];
                if (evidence == null || evidence.AttemptCount < 0 ||
                    evidence.AgreementCount < 0 ||
                    evidence.AgreementCount > evidence.AttemptCount ||
                    (evidence.Result != null && evidence.AgreementCount == 0) ||
                    (evidence.Result == null && evidence.AgreementCount > 0) ||
                    HasInvalidCorrectionCounts(evidence.Result))
                {
                    throw new BarcodeValidationException("Candidate decode evidence is invalid.");
                }
            }

            index = index + 1;
        }
    }

    static bool HasInvalidCorrectionCounts(BarcodeDecodeResult? result)
    {
        if (result == null || result.Metadata == null)
        {
            return false;
        }

        return result.Metadata.CorrectedErrorCount < 0 ||
            result.Metadata.ErasureCount < 0;
    }
}

namespace qrcode;

internal class FinderClusterer
{
    internal static FinderCandidate[] Merge(
        FinderCandidate[] candidates,
        QrDetectionOptions options)
    {
        if (candidates == null)
        {
            throw new QrInputException("The finder candidates are required.", "candidates");
        }

        if (options == null)
        {
            throw new QrInputException("The detection options are required.", "options");
        }

        if (options.MaximumCandidates <= 0)
        {
            throw new QrInputException(
                "The maximum candidate count must be positive.",
                "MaximumCandidates");
        }

        if (options.MaximumPyramidLevels <= 0)
        {
            throw new QrInputException(
                "The maximum pyramid level count must be positive.",
                "MaximumPyramidLevels");
        }

        long maximumInputCount = (long)options.MaximumCandidates
            * options.MaximumPyramidLevels
            * 2;
        if (candidates.Length > maximumInputCount)
        {
            int maximum = maximumInputCount > int.MaxValue
                ? int.MaxValue
                : (int)maximumInputCount;
            throw new QrResourceLimitException(
                "The combined finder candidate count exceeds the configured pass limits.",
                "MaximumCandidates",
                candidates.Length,
                maximum);
        }

        FinderCandidate[] ordered = new FinderCandidate[candidates.Length];
        for (int index = 0; index < candidates.Length; index++)
        {
            FinderCandidate candidate = candidates[index];
            ValidateCandidate(candidate);
            ordered[index] = candidate;
        }

        SortByGeometry(ordered);
        global::System.Collections.Generic.List<FinderCluster> clusters =
            new global::System.Collections.Generic.List<FinderCluster>();
        for (int index = 0; index < ordered.Length; index++)
        {
            FinderCandidate candidate = ordered[index];
            int bestCluster = -1;
            double bestScore = double.MaxValue;
            for (int clusterIndex = 0; clusterIndex < clusters.Count; clusterIndex++)
            {
                double score = clusters[clusterIndex].GetMatchScore(candidate);
                if (score >= 0.0 && score < bestScore)
                {
                    bestScore = score;
                    bestCluster = clusterIndex;
                }
            }

            if (bestCluster < 0)
            {
                FinderCluster cluster = new FinderCluster();
                cluster.AddCandidate(candidate);
                clusters.Add(cluster);
            }
            else
            {
                clusters[bestCluster].AddCandidate(candidate);
            }
        }

        FinderCandidate[] merged = new FinderCandidate[clusters.Count];
        for (int index = 0; index < clusters.Count; index++)
        {
            merged[index] = clusters[index].CreateCandidate();
        }

        SortByRank(merged);
        int resultCount = merged.Length;
        if (resultCount > options.MaximumCandidates)
        {
            resultCount = options.MaximumCandidates;
        }

        FinderCandidate[] result = new FinderCandidate[resultCount];
        for (int index = 0; index < resultCount; index++)
        {
            result[index] = merged[index];
        }

        return result;
    }

    static void ValidateCandidate(FinderCandidate candidate)
    {
        if (candidate == null)
        {
            throw new QrInputException(
                "Finder candidate entries cannot be null.",
                "candidates");
        }

        if (!IsFinite(candidate.CenterX)
            || !IsFinite(candidate.CenterY)
            || !IsFinite(candidate.ModuleSize)
            || !IsFinite(candidate.Confidence)
            || candidate.CenterX < 0.0
            || candidate.CenterY < 0.0
            || candidate.ModuleSize <= 0.0
            || candidate.Confidence < 0.0
            || candidate.Confidence > 1.0
            || candidate.ScaleToSource <= 0
            || candidate.EvidenceCount <= 0
            || candidate.HorizontalEvidenceCount < 0
            || candidate.VerticalEvidenceCount < 0)
        {
            throw new QrInputException(
                "Finder candidate geometry or confidence is invalid.",
                "candidates");
        }

        int directionalEvidence = candidate.HorizontalEvidenceCount
            + candidate.VerticalEvidenceCount;
        if (directionalEvidence != 0 && directionalEvidence != candidate.EvidenceCount)
        {
            throw new QrInputException(
                "Finder candidate evidence counts are inconsistent.",
                "candidates");
        }

        if (candidate.MinimumScaleToSource < 0
            || candidate.MaximumScaleToSource < 0
            || (candidate.MinimumScaleToSource > 0
                && candidate.MaximumScaleToSource > 0
                && candidate.MinimumScaleToSource > candidate.MaximumScaleToSource))
        {
            throw new QrInputException(
                "Finder candidate scale evidence is inconsistent.",
                "candidates");
        }

        int polarity = (int)candidate.Polarity;
        if (polarity < (int)QrPolarity.Automatic
            || polarity > (int)QrPolarity.LightOnDark)
        {
            throw new QrInputException(
                "The finder candidate polarity is not defined.",
                "candidates");
        }

        if (candidate.RegionValidated
            && (!IsFinite(candidate.RegionConfidence)
                || candidate.RegionConfidence <= 0.0
                || candidate.RegionConfidence > 1.0
                || candidate.CenterRegionArea <= 0
                || candidate.OuterRegionArea <= 0
                || !HasFiniteCorners(candidate)))
        {
            throw new QrInputException(
                "Validated finder candidates require four recovered corners.",
                "candidates");
        }
    }

    static bool HasFiniteCorners(FinderCandidate candidate)
    {
        QrQuadrilateral? corners = candidate.FinderCorners;
        if (corners == null)
        {
            return false;
        }

        QrPoint? topLeft = corners.TopLeft;
        QrPoint? topRight = corners.TopRight;
        QrPoint? bottomRight = corners.BottomRight;
        QrPoint? bottomLeft = corners.BottomLeft;
        if (topLeft == null
            || topRight == null
            || bottomRight == null
            || bottomLeft == null)
        {
            return false;
        }

        return IsFinite(topLeft.X)
            && IsFinite(topLeft.Y)
            && IsFinite(topRight.X)
            && IsFinite(topRight.Y)
            && IsFinite(bottomRight.X)
            && IsFinite(bottomRight.Y)
            && IsFinite(bottomLeft.X)
            && IsFinite(bottomLeft.Y);
    }

    static void SortByGeometry(FinderCandidate[] candidates)
    {
        for (int index = 1; index < candidates.Length; index++)
        {
            FinderCandidate value = candidates[index];
            int position = index;
            while (position > 0 && GeometryComesBefore(value, candidates[position - 1]))
            {
                candidates[position] = candidates[position - 1];
                position--;
            }

            candidates[position] = value;
        }
    }

    static bool GeometryComesBefore(FinderCandidate left, FinderCandidate right)
    {
        if (left.CenterY != right.CenterY)
        {
            return left.CenterY < right.CenterY;
        }

        if (left.CenterX != right.CenterX)
        {
            return left.CenterX < right.CenterX;
        }

        if (left.ModuleSize != right.ModuleSize)
        {
            return left.ModuleSize < right.ModuleSize;
        }

        if (left.ScaleToSource != right.ScaleToSource)
        {
            return left.ScaleToSource < right.ScaleToSource;
        }

        return (int)left.Polarity < (int)right.Polarity;
    }

    static void SortByRank(FinderCandidate[] candidates)
    {
        for (int index = 1; index < candidates.Length; index++)
        {
            FinderCandidate value = candidates[index];
            int position = index;
            while (position > 0 && RankComesBefore(value, candidates[position - 1]))
            {
                candidates[position] = candidates[position - 1];
                position--;
            }

            candidates[position] = value;
        }
    }

    static bool RankComesBefore(FinderCandidate left, FinderCandidate right)
    {
        if (left.Confidence != right.Confidence)
        {
            return left.Confidence > right.Confidence;
        }

        if (left.EvidenceCount != right.EvidenceCount)
        {
            return left.EvidenceCount > right.EvidenceCount;
        }

        if (left.CenterY != right.CenterY)
        {
            return left.CenterY < right.CenterY;
        }

        return left.CenterX < right.CenterX;
    }

    static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
}

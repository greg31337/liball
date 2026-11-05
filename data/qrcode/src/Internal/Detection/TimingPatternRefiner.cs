namespace qrcode;

internal class TimingPatternRefiner
{
    internal const int VersionSearchRadius = 2;
    internal const double MinimumAxisConfidence = 0.65;
    internal const double MinimumCombinedConfidence = 0.75;
    internal const double MinimumPhaseMargin = 0.2;
    internal const double MaximumOppositePhaseConfidence = 0.1;

    internal static TimingPatternRefinementResult Refine(
        QrRaster raster,
        FinderGroup group,
        QrDetectionOptions options)
    {
        BinaryMorphology.ValidateRaster(raster, options);
        ValidateGroup(group);
        TimingPatternRefinementResult fallback = CreateFallback(group);
        bool allowModel2 = options.AllowModel2 && group.Model2Possible;
        bool allowLegacy = options.AllowLegacyModel1 && group.LegacyModel1Possible;
        if (!allowModel2 && !allowLegacy)
        {
            return fallback;
        }

        int maximumVersion = allowModel2
            ? Model2VersionTable.MaximumVersion
            : LegacyModel1Table.MaximumVersion;
        int minimumVersion = group.EstimatedVersion - VersionSearchRadius;
        int maximumTrialVersion = group.EstimatedVersion + VersionSearchRadius;
        if (minimumVersion < Model2VersionTable.MinimumVersion)
        {
            minimumVersion = Model2VersionTable.MinimumVersion;
        }

        if (maximumTrialVersion > maximumVersion)
        {
            maximumTrialVersion = maximumVersion;
        }

        if (minimumVersion > maximumTrialVersion)
        {
            return fallback;
        }

        TimingPatternTrial? best = null;
        TimingPatternTrial? original = null;
        int versionsEvaluated = 0;
        for (int version = minimumVersion; version <= maximumTrialVersion; version++)
        {
            TimingPatternTrial trial = EvaluateVersion(
                raster,
                group,
                version,
                allowModel2);
            versionsEvaluated++;
            if (version == group.EstimatedVersion)
            {
                original = trial;
            }

            if (best == null || TrialComesBefore(trial, best, group.EstimatedVersion))
            {
                best = trial;
            }
        }

        if (best == null || original == null)
        {
            fallback.VersionsEvaluated = versionsEvaluated;
            return fallback;
        }

        bool oppositePhaseAtEstimatedDimension =
            original.HorizontalConfidence <= MaximumOppositePhaseConfidence
            && original.VerticalConfidence <= MaximumOppositePhaseConfidence;
        bool accepted = !oppositePhaseAtEstimatedDimension
            && best.HorizontalConfidence >= MinimumAxisConfidence
            && best.VerticalConfidence >= MinimumAxisConfidence
            && best.Confidence >= MinimumCombinedConfidence
            && best.HorizontalPhaseMargin >= MinimumPhaseMargin
            && best.VerticalPhaseMargin >= MinimumPhaseMargin;
        TimingPatternTrial reported = accepted ? best : original;
        int refinedVersion = accepted ? best.Version : group.EstimatedVersion;
        int refinedDimension = accepted ? best.Dimension : group.EstimatedDimension;
        double horizontalDistance = Distance(group.TopLeft, group.TopRight);
        double verticalDistance = Distance(group.TopLeft, group.BottomLeft);
        return new TimingPatternRefinementResult
        {
            Success = accepted,
            UsedFallback = !accepted,
            DimensionChanged = accepted && refinedDimension != group.EstimatedDimension,
            OriginalVersion = group.EstimatedVersion,
            OriginalDimension = group.EstimatedDimension,
            RefinedVersion = refinedVersion,
            RefinedDimension = refinedDimension,
            VersionsEvaluated = versionsEvaluated,
            HorizontalSampleCount = reported.HorizontalSampleCount,
            VerticalSampleCount = reported.VerticalSampleCount,
            HorizontalConfidence = reported.HorizontalConfidence,
            VerticalConfidence = reported.VerticalConfidence,
            HorizontalPhaseMargin = reported.HorizontalPhaseMargin,
            VerticalPhaseMargin = reported.VerticalPhaseMargin,
            Confidence = reported.Confidence,
            HorizontalModuleSize = horizontalDistance / (refinedDimension - 7),
            VerticalModuleSize = verticalDistance / (refinedDimension - 7)
        };
    }

    static TimingPatternTrial EvaluateVersion(
        QrRaster raster,
        FinderGroup group,
        int version,
        bool skipModel2AlignmentInterruptions)
    {
        int dimension = Model2VersionTable.GetDimension(version);
        double columnStepX = (group.TopRight.CenterX - group.TopLeft.CenterX)
            / (dimension - 7);
        double columnStepY = (group.TopRight.CenterY - group.TopLeft.CenterY)
            / (dimension - 7);
        double rowStepX = (group.BottomLeft.CenterX - group.TopLeft.CenterX)
            / (dimension - 7);
        double rowStepY = (group.BottomLeft.CenterY - group.TopLeft.CenterY)
            / (dimension - 7);
        int horizontalMatches = 0;
        int verticalMatches = 0;
        int horizontalSamples = 0;
        int verticalSamples = 0;
        for (int index = 8; index < dimension - 8; index++)
        {
            if (skipModel2AlignmentInterruptions
                && IsAlignmentInterruption(version, index))
            {
                continue;
            }

            bool expectedDark = index % 2 == 0;
            int horizontal = SampleLogicalModule(
                raster,
                group,
                Model2PlacementTable.TimingPatternIndex,
                index,
                columnStepX,
                columnStepY,
                rowStepX,
                rowStepY);
            int vertical = SampleLogicalModule(
                raster,
                group,
                index,
                Model2PlacementTable.TimingPatternIndex,
                columnStepX,
                columnStepY,
                rowStepX,
                rowStepY);
            horizontalSamples++;
            verticalSamples++;
            if ((horizontal == 1) == expectedDark)
            {
                horizontalMatches++;
            }

            if ((vertical == 1) == expectedDark)
            {
                verticalMatches++;
            }
        }

        double horizontalConfidence = horizontalSamples == 0
            ? 0.0
            : (double)horizontalMatches / horizontalSamples;
        double verticalConfidence = verticalSamples == 0
            ? 0.0
            : (double)verticalMatches / verticalSamples;
        return new TimingPatternTrial
        {
            Version = version,
            Dimension = dimension,
            HorizontalSampleCount = horizontalSamples,
            VerticalSampleCount = verticalSamples,
            HorizontalConfidence = horizontalConfidence,
            VerticalConfidence = verticalConfidence,
            Confidence = (horizontalConfidence + verticalConfidence) / 2.0,
            HorizontalPhaseMargin = horizontalConfidence * 2.0 - 1.0,
            VerticalPhaseMargin = verticalConfidence * 2.0 - 1.0
        };
    }

    static int SampleLogicalModule(
        QrRaster raster,
        FinderGroup group,
        int row,
        int column,
        double columnStepX,
        double columnStepY,
        double rowStepX,
        double rowStepY)
    {
        double x = group.TopLeft.CenterX
            + (column - 3) * columnStepX
            + (row - 3) * rowStepX;
        double y = group.TopLeft.CenterY
            + (column - 3) * columnStepY
            + (row - 3) * rowStepY;
        if (x < 0.0 || y < 0.0 || x >= raster.Width || y >= raster.Height)
        {
            return -1;
        }

        return raster.Pixels[(int)y * raster.Stride + (int)x] == 0 ? 1 : 0;
    }

    static bool IsAlignmentInterruption(int version, int timingIndex)
    {
        int[] positions = AlignmentPositionTable.GetPositions(version);
        for (int index = 1; index < positions.Length - 1; index++)
        {
            int distance = timingIndex - positions[index];
            if (distance < 0)
            {
                distance = -distance;
            }

            if (distance <= 2)
            {
                return true;
            }
        }

        return false;
    }

    static bool TrialComesBefore(
        TimingPatternTrial left,
        TimingPatternTrial right,
        int estimatedVersion)
    {
        if (left.Confidence != right.Confidence)
        {
            return left.Confidence > right.Confidence;
        }

        double leftMinimum = left.HorizontalConfidence;
        if (left.VerticalConfidence < leftMinimum)
        {
            leftMinimum = left.VerticalConfidence;
        }

        double rightMinimum = right.HorizontalConfidence;
        if (right.VerticalConfidence < rightMinimum)
        {
            rightMinimum = right.VerticalConfidence;
        }

        if (leftMinimum != rightMinimum)
        {
            return leftMinimum > rightMinimum;
        }

        int leftDistance = left.Version - estimatedVersion;
        if (leftDistance < 0)
        {
            leftDistance = -leftDistance;
        }

        int rightDistance = right.Version - estimatedVersion;
        if (rightDistance < 0)
        {
            rightDistance = -rightDistance;
        }

        if (leftDistance != rightDistance)
        {
            return leftDistance < rightDistance;
        }

        return left.Version < right.Version;
    }

    static TimingPatternRefinementResult CreateFallback(FinderGroup group)
    {
        double horizontalDistance = Distance(group.TopLeft, group.TopRight);
        double verticalDistance = Distance(group.TopLeft, group.BottomLeft);
        return new TimingPatternRefinementResult
        {
            Success = false,
            UsedFallback = true,
            OriginalVersion = group.EstimatedVersion,
            OriginalDimension = group.EstimatedDimension,
            RefinedVersion = group.EstimatedVersion,
            RefinedDimension = group.EstimatedDimension,
            HorizontalModuleSize = horizontalDistance / (group.EstimatedDimension - 7),
            VerticalModuleSize = verticalDistance / (group.EstimatedDimension - 7)
        };
    }

    static void ValidateGroup(FinderGroup group)
    {
        if (group == null
            || group.TopLeft == null
            || group.TopRight == null
            || group.BottomLeft == null
            || !IsFinite(group.TopLeft.CenterX)
            || !IsFinite(group.TopLeft.CenterY)
            || !IsFinite(group.TopRight.CenterX)
            || !IsFinite(group.TopRight.CenterY)
            || !IsFinite(group.BottomLeft.CenterX)
            || !IsFinite(group.BottomLeft.CenterY)
            || !IsFinite(group.ModuleSize)
            || group.ModuleSize <= 0.0
            || group.EstimatedVersion < Model2VersionTable.MinimumVersion
            || group.EstimatedVersion > Model2VersionTable.MaximumVersion
            || group.EstimatedDimension
                != Model2VersionTable.GetDimension(group.EstimatedVersion))
        {
            throw new QrInputException(
                "The finder group geometry or version estimate is invalid.",
                "group");
        }

        if (Distance(group.TopLeft, group.TopRight) <= 0.0
            || Distance(group.TopLeft, group.BottomLeft) <= 0.0)
        {
            throw new QrInputException(
                "The finder group axes must have positive length.",
                "group");
        }
    }

    static double Distance(FinderCandidate first, FinderCandidate second)
    {
        double horizontal = second.CenterX - first.CenterX;
        double vertical = second.CenterY - first.CenterY;
        return global::System.Math.Sqrt(horizontal * horizontal + vertical * vertical);
    }

    static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
}

namespace qrcode;

internal class FinderGrouper
{
    internal const double MaximumRightAngleCosine = 0.3;
    internal const double MinimumModuleRatio = 0.6;
    internal const double MinimumLegRatio = 0.5;
    internal const double MinimumCenterDistanceModules = 10.0;

    internal static FinderGroup[] Group(
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

        if (candidates.Length > options.MaximumCandidates)
        {
            throw new QrResourceLimitException(
                "The finder candidate count exceeds the configured limit.",
                "MaximumCandidates",
                candidates.Length,
                options.MaximumCandidates);
        }

        for (int index = 0; index < candidates.Length; index++)
        {
            ValidateCandidate(candidates[index]);
        }

        global::System.Collections.Generic.List<FinderGroup> groups =
            new global::System.Collections.Generic.List<FinderGroup>();
        for (int first = 0; first < candidates.Length - 2; first++)
        {
            for (int second = first + 1; second < candidates.Length - 1; second++)
            {
                for (int third = second + 1; third < candidates.Length; third++)
                {
                    FinderGroup? group = TryCreateGroup(
                        candidates[first],
                        candidates[second],
                        candidates[third],
                        options);
                    if (group != null)
                    {
                        AddBoundedGroup(groups, group, options.MaximumCandidates);
                    }
                }
            }
        }

        FinderGroup[] result = groups.ToArray();
        SortGroups(result);
        return result;
    }

    static FinderGroup? TryCreateGroup(
        FinderCandidate first,
        FinderCandidate second,
        FinderCandidate third,
        QrDetectionOptions options)
    {
        double firstSecond = SquaredDistance(first, second);
        double firstThird = SquaredDistance(first, third);
        double secondThird = SquaredDistance(second, third);
        FinderCandidate anchor;
        FinderCandidate firstArm;
        FinderCandidate secondArm;
        if (secondThird >= firstSecond && secondThird >= firstThird)
        {
            anchor = first;
            firstArm = second;
            secondArm = third;
        }
        else if (firstThird >= firstSecond)
        {
            anchor = second;
            firstArm = first;
            secondArm = third;
        }
        else
        {
            anchor = third;
            firstArm = first;
            secondArm = second;
        }

        double firstVectorX = firstArm.CenterX - anchor.CenterX;
        double firstVectorY = firstArm.CenterY - anchor.CenterY;
        double secondVectorX = secondArm.CenterX - anchor.CenterX;
        double secondVectorY = secondArm.CenterY - anchor.CenterY;
        double firstDistance = global::System.Math.Sqrt(
            firstVectorX * firstVectorX + firstVectorY * firstVectorY);
        double secondDistance = global::System.Math.Sqrt(
            secondVectorX * secondVectorX + secondVectorY * secondVectorY);
        if (firstDistance <= 0.0 || secondDistance <= 0.0)
        {
            return null;
        }

        double dot = firstVectorX * secondVectorX + firstVectorY * secondVectorY;
        double rightAngleCosine = global::System.Math.Abs(dot)
            / (firstDistance * secondDistance);
        if (rightAngleCosine > MaximumRightAngleCosine)
        {
            return null;
        }

        double minimumLeg = firstDistance;
        double maximumLeg = secondDistance;
        if (firstDistance > secondDistance)
        {
            minimumLeg = secondDistance;
            maximumLeg = firstDistance;
        }

        if (minimumLeg / maximumLeg < MinimumLegRatio)
        {
            return null;
        }

        double minimumModule = anchor.ModuleSize;
        double maximumModule = anchor.ModuleSize;
        minimumModule = SelectMinimum(minimumModule, firstArm.ModuleSize);
        minimumModule = SelectMinimum(minimumModule, secondArm.ModuleSize);
        maximumModule = SelectMaximum(maximumModule, firstArm.ModuleSize);
        maximumModule = SelectMaximum(maximumModule, secondArm.ModuleSize);
        if (minimumModule / maximumModule < MinimumModuleRatio
            || minimumLeg / maximumModule < MinimumCenterDistanceModules)
        {
            return null;
        }

        FinderCandidate topRight = firstArm;
        FinderCandidate bottomLeft = secondArm;
        double cross = firstVectorX * secondVectorY - firstVectorY * secondVectorX;
        if (cross < 0.0)
        {
            topRight = secondArm;
            bottomLeft = firstArm;
        }

        QrPolarity polarity = ResolvePolarity(anchor, topRight, bottomLeft);
        if ((int)polarity < 0)
        {
            return null;
        }

        double horizontalDistance = Distance(anchor, topRight);
        double verticalDistance = Distance(anchor, bottomLeft);
        double horizontalModule = (anchor.ModuleSize + topRight.ModuleSize) / 2.0;
        double verticalModule = (anchor.ModuleSize + bottomLeft.ModuleSize) / 2.0;
        DimensionEstimate estimate = DimensionEstimator.EstimateSquare(
            horizontalDistance,
            verticalDistance,
            horizontalModule,
            verticalModule,
            options.AllowModel2,
            options.AllowLegacyModel1);
        if (!estimate.Success)
        {
            return null;
        }

        double candidateConfidence = (anchor.Confidence
            + topRight.Confidence
            + bottomLeft.Confidence) / 3.0;
        double angleScore = 1.0 - rightAngleCosine / MaximumRightAngleCosine;
        double moduleScore = minimumModule / maximumModule;
        double legScore = minimumLeg / maximumLeg;
        double confidence = (candidateConfidence
            + angleScore
            + moduleScore
            + legScore
            + estimate.Confidence) / 5.0;
        FinderGroup group = new FinderGroup(anchor, topRight, bottomLeft)
        {
            EstimatedBottomRight = new QrPoint
            {
                X = topRight.CenterX + bottomLeft.CenterX - anchor.CenterX,
                Y = topRight.CenterY + bottomLeft.CenterY - anchor.CenterY
            },
            EstimatedSymbolKind = estimate.Model2Possible
                ? QrSymbolKind.Model2
                : QrSymbolKind.LegacyModel1,
            Model2Possible = estimate.Model2Possible,
            LegacyModel1Possible = estimate.LegacyModel1Possible,
            EstimatedVersion = estimate.Version,
            EstimatedDimension = estimate.Dimension,
            ModuleSize = (anchor.ModuleSize
                + topRight.ModuleSize
                + bottomLeft.ModuleSize) / 3.0,
            HorizontalDistance = horizontalDistance,
            VerticalDistance = verticalDistance,
            RightAngleCosine = rightAngleCosine,
            OrientationRadians = global::System.Math.Atan2(
                topRight.CenterY - anchor.CenterY,
                topRight.CenterX - anchor.CenterX),
            Confidence = confidence,
            Polarity = polarity
        };
        return group;
    }

    static QrPolarity ResolvePolarity(
        FinderCandidate first,
        FinderCandidate second,
        FinderCandidate third)
    {
        QrPolarity result = QrPolarity.Automatic;
        if (first.Polarity != QrPolarity.Automatic)
        {
            result = first.Polarity;
        }

        if (second.Polarity != QrPolarity.Automatic)
        {
            if (result != QrPolarity.Automatic && result != second.Polarity)
            {
                return (QrPolarity)(-1);
            }

            result = second.Polarity;
        }

        if (third.Polarity != QrPolarity.Automatic)
        {
            if (result != QrPolarity.Automatic && result != third.Polarity)
            {
                return (QrPolarity)(-1);
            }

            result = third.Polarity;
        }

        return result;
    }

    static void AddBoundedGroup(
        global::System.Collections.Generic.List<FinderGroup> groups,
        FinderGroup group,
        int maximumGroups)
    {
        if (groups.Count < maximumGroups)
        {
            groups.Add(group);
            return;
        }

        int worstIndex = 0;
        for (int index = 1; index < groups.Count; index++)
        {
            if (GroupComesBefore(groups[worstIndex], groups[index]))
            {
                worstIndex = index;
            }
        }

        if (GroupComesBefore(group, groups[worstIndex]))
        {
            groups[worstIndex] = group;
        }
    }

    static void SortGroups(FinderGroup[] groups)
    {
        for (int index = 1; index < groups.Length; index++)
        {
            FinderGroup value = groups[index];
            int position = index;
            while (position > 0 && GroupComesBefore(value, groups[position - 1]))
            {
                groups[position] = groups[position - 1];
                position--;
            }

            groups[position] = value;
        }
    }

    static bool GroupComesBefore(FinderGroup left, FinderGroup right)
    {
        if (left.Confidence != right.Confidence)
        {
            return left.Confidence > right.Confidence;
        }

        if (left.TopLeft.CenterY != right.TopLeft.CenterY)
        {
            return left.TopLeft.CenterY < right.TopLeft.CenterY;
        }

        if (left.TopLeft.CenterX != right.TopLeft.CenterX)
        {
            return left.TopLeft.CenterX < right.TopLeft.CenterX;
        }

        return left.EstimatedDimension < right.EstimatedDimension;
    }

    static void ValidateCandidate(FinderCandidate candidate)
    {
        if (candidate == null
            || !IsFinite(candidate.CenterX)
            || !IsFinite(candidate.CenterY)
            || !IsFinite(candidate.ModuleSize)
            || !IsFinite(candidate.Confidence)
            || candidate.CenterX < 0.0
            || candidate.CenterY < 0.0
            || candidate.ModuleSize <= 0.0
            || candidate.Confidence < 0.0
            || candidate.Confidence > 1.0
            || !candidate.RegionValidated
            || !IsFinite(candidate.RegionConfidence)
            || candidate.RegionConfidence <= 0.0
            || candidate.RegionConfidence > 1.0
            || candidate.FinderCorners == null)
        {
            throw new QrInputException(
                "Finder candidate geometry or confidence is invalid.",
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
    }

    static double SquaredDistance(FinderCandidate first, FinderCandidate second)
    {
        double horizontal = second.CenterX - first.CenterX;
        double vertical = second.CenterY - first.CenterY;
        return horizontal * horizontal + vertical * vertical;
    }

    static double Distance(FinderCandidate first, FinderCandidate second)
    {
        return global::System.Math.Sqrt(SquaredDistance(first, second));
    }

    static double SelectMinimum(double first, double second)
    {
        return first < second ? first : second;
    }

    static double SelectMaximum(double first, double second)
    {
        return first > second ? first : second;
    }

    static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
}

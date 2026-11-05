namespace barcode;

internal class Pdf417FinderEvidence
{
    internal BarcodeCandidate Candidate = new BarcodeCandidate();
    internal BarcodeQuadrilateral Location = new BarcodeQuadrilateral();
    internal BarcodePolarity Polarity;
    internal Pdf417Variant Variant;
    internal double AngleDegrees;
    internal double StartLineScore;
    internal double StopLineScore;
    internal double DimensionScore;
    internal double LineCoverage;
    internal double ModuleSize;
    internal int EstimatedColumnCount;
    internal int RowEvidenceCount;
    internal double Confidence;
}

internal class Pdf417GuardPair
{
    internal int StartIndex;
    internal int StartEndIndex;
    internal int StopIndex;
    internal int StopEndIndex;
    internal double StartVariance;
    internal double StopVariance;
    internal double ModuleSize;
    internal int EstimatedColumnCount;
    internal double DimensionScore;
}

internal class Pdf417GuardObservation
{
    internal double Offset;
    internal BarcodePoint Left = new BarcodePoint();
    internal BarcodePoint Right = new BarcodePoint();
    internal double StartVariance;
    internal double StopVariance;
    internal double ModuleSize;
    internal int EstimatedColumnCount;
    internal double DimensionScore;
    internal Pdf417Variant Variant;
}

internal class Pdf417AngleSearchResult
{
    internal double AngleDegrees;
    internal BarcodePolarity Polarity;
    internal Pdf417GuardObservation[] Observations =
        new Pdf417GuardObservation[0];
    internal double Score;
}

internal static class Pdf417ImageFinder
{
    internal const int MinimumImageDimension = 8;
    internal const int MinimumRowEvidenceCount = 3;
    internal const int MaximumSampledLineCount = 256;
    internal const int MaximumRefinementAdjustment = 24;
    internal const double CoarseAngleStepDegrees = 4.0;
    internal const double FineAngleStepDegrees = 0.5;
    internal const double MaximumAverageGuardVariance = 0.30;
    internal const double MaximumIndividualGuardVariance = 0.90;
    internal const double MaximumModuleSizeDifference = 0.45;
    internal const double MinimumStartLineScore = 0.55;
    internal const double MinimumStopLineScore = 0.55;
    internal const double MinimumDimensionScore = 0.45;
    internal const double MinimumConfidence = 0.70;

    internal static Pdf417FinderEvidence? Locate(
        ModuleMatrix image,
        BarcodePolarity polarity)
    {
        ValidateFinderInput(image, polarity);
        return LocateFinderVariant(
            image,
            polarity,
            Pdf417Variant.Standard);
    }

    internal static Pdf417FinderEvidence? LocateCompact(
        ModuleMatrix image,
        BarcodePolarity polarity)
    {
        ValidateFinderInput(image, polarity);
        return LocateFinderVariant(
            image,
            polarity,
            Pdf417Variant.Compact);
    }

    static Pdf417FinderEvidence? LocateFinderVariant(
        ModuleMatrix image,
        BarcodePolarity polarity,
        Pdf417Variant variant)
    {
        Pdf417AngleSearchResult? best = null;
        if (polarity == BarcodePolarity.Automatic)
        {
            best = SearchAllAngles(
                image,
                BarcodePolarity.DarkOnLight,
                variant);
            Pdf417AngleSearchResult? inverted = SearchAllAngles(
                image,
                BarcodePolarity.LightOnDark,
                variant);
            if (inverted != null
                && (best == null || inverted.Score > best.Score))
            {
                best = inverted;
            }
        }
        else
        {
            best = SearchAllAngles(image, polarity, variant);
        }

        if (best == null)
        {
            return null;
        }

        Pdf417FinderEvidence evidence = CreateFinderEvidence(
            best.Observations,
            best.Polarity);
        if (!MeetsFinderThresholds(evidence))
        {
            return null;
        }

        return evidence;
    }

    internal static Pdf417FinderEvidence? EvaluateQuadrilateral(
        ModuleMatrix image,
        BarcodeQuadrilateral quadrilateral,
        BarcodePolarity polarity)
    {
        ValidateFinderInput(image, polarity);
        GeometryMath.ValidateQuadrilateral(quadrilateral);
        Pdf417FinderEvidence? evidence = MeasureCandidateQuadrilateral(
            image,
            quadrilateral,
            polarity);
        if (evidence == null || !MeetsFinderThresholds(evidence))
        {
            return null;
        }

        return evidence;
    }

    internal static Pdf417FinderEvidence MeasureQuadrilateral(
        ModuleMatrix image,
        BarcodeQuadrilateral quadrilateral,
        BarcodePolarity polarity)
    {
        ValidateFinderInput(image, polarity);
        GeometryMath.ValidateQuadrilateral(quadrilateral);
        Pdf417FinderEvidence? evidence = MeasureCandidateQuadrilateral(
            image,
            quadrilateral,
            polarity);
        if (evidence == null)
        {
            throw new BarcodeDetectionException("PDF417 start and stop evidence could not be measured for the quadrilateral.");
        }

        return evidence;
    }

    internal static Pdf417FinderEvidence? RefineQuadrilateral(
        ModuleMatrix image,
        BarcodeQuadrilateral estimate,
        BarcodePolarity polarity,
        int maximumAdjustment)
    {
        ValidateFinderInput(image, polarity);
        GeometryMath.ValidateQuadrilateral(estimate);
        if (maximumAdjustment < 0
            || maximumAdjustment > MaximumRefinementAdjustment)
        {
            throw new BarcodeValidationException("PDF417 finder refinement adjustment is outside the supported range.");
        }

        BarcodeQuadrilateral current = CopyFinderQuadrilateral(estimate);
        Pdf417FinderEvidence? best = MeasureCandidateQuadrilateral(
            image,
            current,
            polarity);
        int iteration = 0;
        bool improved = true;
        while (iteration < maximumAdjustment && improved)
        {
            improved = false;
            int corner = 0;
            while (corner < 4)
            {
                int direction = 0;
                while (direction < 8)
                {
                    BarcodeQuadrilateral adjusted = MoveFinderCorner(
                        current,
                        corner,
                        direction);
                    if (IsUsableFinderQuadrilateral(adjusted))
                    {
                        Pdf417FinderEvidence? candidate =
                            MeasureCandidateQuadrilateral(
                                image,
                                adjusted,
                                polarity);
                        if (candidate != null
                            && (best == null
                                || candidate.Confidence
                                    > best.Confidence + 0.000001))
                        {
                            current = adjusted;
                            best = candidate;
                            improved = true;
                        }
                    }

                    direction = direction + 1;
                }

                corner = corner + 1;
            }

            iteration = iteration + 1;
        }

        if (best == null || !MeetsFinderThresholds(best))
        {
            return null;
        }

        return best;
    }

    static Pdf417AngleSearchResult? SearchAllAngles(
        ModuleMatrix image,
        BarcodePolarity polarity,
        Pdf417Variant variant)
    {
        Pdf417AngleSearchResult? best = null;
        double angle = 0.0;
        while (angle < 360.0)
        {
            Pdf417AngleSearchResult? candidate = MeasureSearchAngle(
                image,
                polarity,
                angle,
                variant);
            if (candidate != null
                && (best == null || candidate.Score > best.Score))
            {
                best = candidate;
            }

            angle = angle + CoarseAngleStepDegrees;
        }

        if (best == null)
        {
            return null;
        }

        double fineStart = best.AngleDegrees - CoarseAngleStepDegrees;
        double fineEnd = best.AngleDegrees + CoarseAngleStepDegrees;
        angle = fineStart;
        while (angle <= fineEnd + GeometryMath.Epsilon)
        {
            double normalized = NormalizeFinderAngle(angle);
            Pdf417AngleSearchResult? candidate = MeasureSearchAngle(
                image,
                polarity,
                normalized,
                variant);
            if (candidate != null && candidate.Score > best.Score)
            {
                best = candidate;
            }

            angle = angle + FineAngleStepDegrees;
        }

        return best;
    }

    static Pdf417AngleSearchResult? MeasureSearchAngle(
        ModuleMatrix image,
        BarcodePolarity polarity,
        double angleDegrees,
        Pdf417Variant variant)
    {
        double radians = angleDegrees * global::System.Math.PI / 180.0;
        double directionX = global::System.Math.Cos(radians);
        double directionY = global::System.Math.Sin(radians);
        double normalX = -directionY;
        double normalY = directionX;
        double[] projections = CalculateImageProjections(
            image,
            directionX,
            directionY,
            normalX,
            normalY);
        double firstOffset = global::System.Math.Ceiling(projections[2]);
        double lastOffset = global::System.Math.Floor(projections[3]);
        double lineStep = 1.0;
        double availableLineCount = lastOffset - firstOffset + 1.0;
        if (availableLineCount > MaximumSampledLineCount)
        {
            lineStep = (availableLineCount - 1.0)
                / (MaximumSampledLineCount - 1.0);
        }

        global::System.Collections.Generic.List<Pdf417GuardObservation> observations =
            new global::System.Collections.Generic.List<Pdf417GuardObservation>();
        double offset = firstOffset;
        int sampledCount = 0;
        while (offset <= lastOffset
            && sampledCount < MaximumSampledLineCount)
        {
            BitRow row = SampleProjectedLine(
                image,
                polarity,
                projections[0],
                projections[1],
                offset,
                directionX,
                directionY,
                normalX,
                normalY);
            Pdf417GuardPair? pair = variant == Pdf417Variant.Compact
                ? FindCompactGuardPair(row)
                : FindGuardPair(row);
            if (pair != null)
            {
                Pdf417GuardObservation observation =
                    CreateProjectedObservation(
                        pair,
                        projections[0],
                        offset,
                        directionX,
                        directionY,
                        normalX,
                        normalY);
                observation.Variant = variant;
                observations.Add(observation);
            }

            offset = offset + lineStep;
            sampledCount = sampledCount + 1;
        }

        Pdf417GuardObservation[] selected = SelectLargestGuardGroup(
            observations,
            lineStep * 2.01);
        if (selected.Length < MinimumRowEvidenceCount)
        {
            return null;
        }

        double varianceTotal = 0.0;
        double dimensionTotal = 0.0;
        int index = 0;
        while (index < selected.Length)
        {
            varianceTotal = varianceTotal
                + selected[index].StartVariance
                + selected[index].StopVariance;
            dimensionTotal = dimensionTotal
                + selected[index].DimensionScore;
            index = index + 1;
        }

        double span = selected[selected.Length - 1].Offset
            - selected[0].Offset + 1.0;
        double coverage = selected.Length / span;
        double averageVariance = varianceTotal / (selected.Length * 2.0);
        double score = (selected.Length * 100.0)
            + (coverage * 20.0)
            + ((dimensionTotal / selected.Length) * 10.0)
            - (averageVariance * 10.0);
        Pdf417AngleSearchResult result = new Pdf417AngleSearchResult();
        result.AngleDegrees = NormalizeFinderAngle(angleDegrees);
        result.Polarity = polarity;
        result.Observations = selected;
        result.Score = score;
        return result;
    }

    static double[] CalculateImageProjections(
        ModuleMatrix image,
        double directionX,
        double directionY,
        double normalX,
        double normalY)
    {
        double[] x = new double[] { 0.0, image.Width - 1.0 };
        double[] y = new double[] { 0.0, image.Height - 1.0 };
        double minimumDirection = double.MaxValue;
        double maximumDirection = double.MinValue;
        double minimumNormal = double.MaxValue;
        double maximumNormal = double.MinValue;
        int xIndex = 0;
        while (xIndex < x.Length)
        {
            int yIndex = 0;
            while (yIndex < y.Length)
            {
                double direction = (x[xIndex] * directionX)
                    + (y[yIndex] * directionY);
                double normal = (x[xIndex] * normalX)
                    + (y[yIndex] * normalY);
                minimumDirection = global::System.Math.Min(
                    minimumDirection,
                    direction);
                maximumDirection = global::System.Math.Max(
                    maximumDirection,
                    direction);
                minimumNormal = global::System.Math.Min(minimumNormal, normal);
                maximumNormal = global::System.Math.Max(maximumNormal, normal);
                yIndex = yIndex + 1;
            }

            xIndex = xIndex + 1;
        }

        return new double[]
        {
            minimumDirection,
            maximumDirection,
            minimumNormal,
            maximumNormal
        };
    }

    static BitRow SampleProjectedLine(
        ModuleMatrix image,
        BarcodePolarity polarity,
        double firstProjection,
        double lastProjection,
        double offset,
        double directionX,
        double directionY,
        double normalX,
        double normalY)
    {
        int length = (int)global::System.Math.Ceiling(
            lastProjection - firstProjection) + 1;
        BitRow row = new BitRow(length);
        int index = 0;
        while (index < length)
        {
            double projection = firstProjection + index;
            int x = (int)global::System.Math.Round(
                (directionX * projection) + (normalX * offset));
            int y = (int)global::System.Math.Round(
                (directionY * projection) + (normalY * offset));
            if (x >= 0 && x < image.Width && y >= 0 && y < image.Height)
            {
                bool value = image.GetModule(x, y);
                bool foreground = polarity == BarcodePolarity.DarkOnLight
                    ? value
                    : !value;
                if (foreground)
                {
                    row.SetBit(index, true);
                }
            }

            index = index + 1;
        }

        return row;
    }

    static Pdf417GuardPair? FindGuardPair(BitRow row)
    {
        int[] startRuns = CreateFinderPatternRuns(
            Pdf417CodewordTable.StartPatternBits,
            Pdf417CodewordTable.StartPatternModuleCount,
            Pdf417CodewordTable.RunsPerCodeword);
        int[] stopRuns = CreateFinderPatternRuns(
            Pdf417CodewordTable.StopPatternBits,
            Pdf417CodewordTable.StopPatternModuleCount,
            9);
        int[] reverseStopRuns = ReverseFinderRuns(stopRuns);
        int[]? start = PatternMatcher.FindForward(
            row,
            startRuns,
            true,
            MaximumAverageGuardVariance,
            MaximumIndividualGuardVariance);
        int[]? stop = PatternMatcher.FindReverse(
            row,
            reverseStopRuns,
            true,
            MaximumAverageGuardVariance,
            MaximumIndividualGuardVariance);
        if (start == null || stop == null || start[1] >= stop[0])
        {
            return null;
        }

        int[] observedStart = RunLength.ExtractForward(
            row,
            start[0],
            startRuns.Length);
        int[] observedStop = RunLength.ExtractForward(
            row,
            stop[0],
            stopRuns.Length);
        if (observedStart.Length != startRuns.Length
            || observedStop.Length != stopRuns.Length)
        {
            return null;
        }

        double startVariance = PatternMatcher.CalculateNormalizedVariance(
            observedStart,
            startRuns,
            MaximumIndividualGuardVariance);
        double stopVariance = PatternMatcher.CalculateNormalizedVariance(
            observedStop,
            stopRuns,
            MaximumIndividualGuardVariance);
        int startWidth = PatternMatcher.SumPositiveRuns(observedStart);
        int stopWidth = PatternMatcher.SumPositiveRuns(observedStop);
        double startModule = startWidth
            / (double)Pdf417CodewordTable.StartPatternModuleCount;
        double stopModule = stopWidth
            / (double)Pdf417CodewordTable.StopPatternModuleCount;
        double moduleDifference = global::System.Math.Abs(
            startModule - stopModule)
            / global::System.Math.Max(startModule, stopModule);
        if (moduleDifference > MaximumModuleSizeDifference)
        {
            return null;
        }

        double moduleSize = (startModule + stopModule) * 0.5;
        double observedModules = (stop[1] - start[0]) / moduleSize;
        double columnValue = (observedModules
            - Pdf417SymbolPlanner.StandardRowOverheadModules)
            / Pdf417SymbolPlanner.CodewordWidthModules;
        int columns = (int)global::System.Math.Round(columnValue);
        if (columns < Pdf417SymbolPlanner.MinimumColumns
            || columns > Pdf417SymbolPlanner.MaximumColumns)
        {
            return null;
        }

        double dimensionError = global::System.Math.Abs(columnValue - columns);
        double dimensionScore = 1.0 - (dimensionError / 0.75);
        if (dimensionScore < 0.0)
        {
            dimensionScore = 0.0;
        }

        Pdf417GuardPair pair = new Pdf417GuardPair();
        pair.StartIndex = start[0];
        pair.StartEndIndex = start[1];
        pair.StopIndex = stop[0];
        pair.StopEndIndex = stop[1];
        pair.StartVariance = startVariance;
        pair.StopVariance = stopVariance;
        pair.ModuleSize = moduleSize;
        pair.EstimatedColumnCount = columns;
        pair.DimensionScore = dimensionScore;
        return pair;
    }

    static Pdf417GuardPair? FindCompactGuardPair(BitRow row)
    {
        int[] startRuns = CreateFinderPatternRuns(
            Pdf417CodewordTable.StartPatternBits,
            Pdf417CodewordTable.StartPatternModuleCount,
            Pdf417CodewordTable.RunsPerCodeword);
        int[]? start = PatternMatcher.FindForward(
            row,
            startRuns,
            true,
            MaximumAverageGuardVariance,
            MaximumIndividualGuardVariance);
        if (start == null)
        {
            return null;
        }

        int[] observedStart = RunLength.ExtractForward(
            row,
            start[0],
            startRuns.Length);
        if (observedStart.Length != startRuns.Length)
        {
            return null;
        }

        int terminalEnd = row.Length;
        while (terminalEnd > 0 && !row.ReadBitUnchecked(terminalEnd - 1))
        {
            terminalEnd = terminalEnd - 1;
        }

        int terminalStart = terminalEnd;
        while (terminalStart > 0 && row.ReadBitUnchecked(terminalStart - 1))
        {
            terminalStart = terminalStart - 1;
        }

        if (terminalStart <= start[1])
        {
            return null;
        }

        double startVariance = PatternMatcher.CalculateNormalizedVariance(
            observedStart,
            startRuns,
            MaximumIndividualGuardVariance);
        int startWidth = PatternMatcher.SumPositiveRuns(observedStart);
        double moduleSize = startWidth
            / (double)Pdf417CodewordTable.StartPatternModuleCount;
        int terminalWidth = terminalEnd - terminalStart;
        double terminalVariance = global::System.Math.Abs(
            terminalWidth - moduleSize) / moduleSize;
        if (terminalVariance > MaximumModuleSizeDifference)
        {
            return null;
        }

        double observedModules = (terminalEnd - start[0]) / moduleSize;
        double columnValue = (observedModules
            - Pdf417SymbolPlanner.CompactRowOverheadModules)
            / Pdf417SymbolPlanner.CodewordWidthModules;
        int columns = (int)global::System.Math.Round(columnValue);
        if (columns < Pdf417SymbolPlanner.MinimumColumns
            || columns > Pdf417SymbolPlanner.MaximumColumns)
        {
            return null;
        }

        double dimensionError = global::System.Math.Abs(columnValue - columns);
        double dimensionScore = 1.0 - (dimensionError / 0.75);
        if (dimensionScore < 0.0)
        {
            dimensionScore = 0.0;
        }

        Pdf417GuardPair pair = new Pdf417GuardPair();
        pair.StartIndex = start[0];
        pair.StartEndIndex = start[1];
        pair.StopIndex = terminalStart;
        pair.StopEndIndex = terminalEnd;
        pair.StartVariance = startVariance;
        pair.StopVariance = terminalVariance;
        pair.ModuleSize = moduleSize;
        pair.EstimatedColumnCount = columns;
        pair.DimensionScore = dimensionScore;
        return pair;
    }

    static int[] CreateFinderPatternRuns(
        int pattern,
        int moduleCount,
        int runCount)
    {
        int[] runs = new int[runCount];
        int current = 1;
        int run = 0;
        int bit = moduleCount - 1;
        while (bit >= 0)
        {
            int value = (pattern >> bit) & 1;
            if (value == current)
            {
                runs[run] = runs[run] + 1;
            }
            else
            {
                run = run + 1;
                if (run >= runCount)
                {
                    throw new BarcodeValidationException("PDF417 guard pattern has too many runs.");
                }

                current = value;
                runs[run] = 1;
            }

            bit = bit - 1;
        }

        if (run != runCount - 1)
        {
            throw new BarcodeValidationException("PDF417 guard pattern has an unexpected run count.");
        }

        return runs;
    }

    static int[] ReverseFinderRuns(int[] runs)
    {
        int[] reversed = new int[runs.Length];
        int index = 0;
        while (index < runs.Length)
        {
            reversed[index] = runs[runs.Length - 1 - index];
            index = index + 1;
        }

        return reversed;
    }

    static Pdf417GuardObservation CreateProjectedObservation(
        Pdf417GuardPair pair,
        double firstProjection,
        double offset,
        double directionX,
        double directionY,
        double normalX,
        double normalY)
    {
        double leftProjection = firstProjection + pair.StartIndex - 0.5;
        double rightProjection = firstProjection + pair.StopEndIndex - 0.5;
        Pdf417GuardObservation observation = new Pdf417GuardObservation();
        observation.Offset = offset;
        observation.Left = GeometryMath.CreatePoint(
            (directionX * leftProjection) + (normalX * offset),
            (directionY * leftProjection) + (normalY * offset));
        observation.Right = GeometryMath.CreatePoint(
            (directionX * rightProjection) + (normalX * offset),
            (directionY * rightProjection) + (normalY * offset));
        observation.StartVariance = pair.StartVariance;
        observation.StopVariance = pair.StopVariance;
        observation.ModuleSize = pair.ModuleSize;
        observation.EstimatedColumnCount = pair.EstimatedColumnCount;
        observation.DimensionScore = pair.DimensionScore;
        return observation;
    }

    static Pdf417GuardObservation[] SelectLargestGuardGroup(
        global::System.Collections.Generic.List<Pdf417GuardObservation> observations,
        double maximumGap)
    {
        if (observations.Count == 0)
        {
            return new Pdf417GuardObservation[0];
        }

        int bestStart = 0;
        int bestCount = 1;
        int currentStart = 0;
        int index = 1;
        while (index <= observations.Count)
        {
            bool continues = index < observations.Count
                && observations[index].Offset
                    - observations[index - 1].Offset <= maximumGap;
            if (!continues)
            {
                int count = index - currentStart;
                if (count > bestCount)
                {
                    bestStart = currentStart;
                    bestCount = count;
                }

                currentStart = index;
            }

            index = index + 1;
        }

        Pdf417GuardObservation[] selected =
            new Pdf417GuardObservation[bestCount];
        index = 0;
        while (index < bestCount)
        {
            selected[index] = observations[bestStart + index];
            index = index + 1;
        }

        return selected;
    }

    static Pdf417FinderEvidence CreateFinderEvidence(
        Pdf417GuardObservation[] observations,
        BarcodePolarity polarity)
    {
        double edgeAdjustment = 0.5;
        if (observations.Length > 1)
        {
            edgeAdjustment = (observations[observations.Length - 1].Offset
                - observations[0].Offset)
                / (observations.Length - 1) * 0.5;
        }

        double firstOffset = observations[0].Offset - edgeAdjustment;
        double lastOffset = observations[observations.Length - 1].Offset
            + edgeAdjustment;
        BarcodePoint topLeft = FitObservationPoint(
            observations,
            true,
            firstOffset);
        BarcodePoint topRight = FitObservationPoint(
            observations,
            false,
            firstOffset);
        BarcodePoint bottomLeft = FitObservationPoint(
            observations,
            true,
            lastOffset);
        BarcodePoint bottomRight = FitObservationPoint(
            observations,
            false,
            lastOffset);
        BarcodeQuadrilateral location = GeometryMath.CreateQuadrilateral(
            topLeft,
            topRight,
            bottomRight,
            bottomLeft);

        double startVariance = 0.0;
        double stopVariance = 0.0;
        double moduleSize = 0.0;
        double dimensionScore = 0.0;
        int[] columnVotes = new int[Pdf417SymbolPlanner.MaximumColumns + 1];
        int index = 0;
        while (index < observations.Length)
        {
            Pdf417GuardObservation observation = observations[index];
            startVariance = startVariance + observation.StartVariance;
            stopVariance = stopVariance + observation.StopVariance;
            moduleSize = moduleSize + observation.ModuleSize;
            dimensionScore = dimensionScore + observation.DimensionScore;
            columnVotes[observation.EstimatedColumnCount] =
                columnVotes[observation.EstimatedColumnCount] + 1;
            index = index + 1;
        }

        double startScore = CalculateFinderPatternScore(
            startVariance / observations.Length);
        double stopScore = CalculateFinderPatternScore(
            stopVariance / observations.Length);
        dimensionScore = dimensionScore / observations.Length;
        double span = observations[observations.Length - 1].Offset
            - observations[0].Offset + 1.0;
        double coverage = observations.Length / span;
        double countScore = observations.Length >= 8
            ? 1.0
            : observations.Length / 8.0;
        double confidence = (startScore * 0.25)
            + (stopScore * 0.25)
            + (dimensionScore * 0.20)
            + (coverage * 0.15)
            + (countScore * 0.15);
        double horizontalX = ((topRight.X - topLeft.X)
            + (bottomRight.X - bottomLeft.X)) * 0.5;
        double horizontalY = ((topRight.Y - topLeft.Y)
            + (bottomRight.Y - bottomLeft.Y)) * 0.5;
        double angle = NormalizeFinderAngle(
            global::System.Math.Atan2(horizontalY, horizontalX)
                * 180.0 / global::System.Math.PI);

        Pdf417FinderEvidence evidence = new Pdf417FinderEvidence();
        evidence.Location = location;
        evidence.Polarity = polarity;
        evidence.Variant = observations[0].Variant;
        evidence.AngleDegrees = angle;
        evidence.StartLineScore = startScore;
        evidence.StopLineScore = stopScore;
        evidence.DimensionScore = dimensionScore;
        evidence.LineCoverage = coverage;
        evidence.ModuleSize = moduleSize / observations.Length;
        evidence.EstimatedColumnCount = SelectFinderColumnCount(columnVotes);
        evidence.RowEvidenceCount = observations.Length;
        evidence.Confidence = confidence;

        BarcodeCandidate candidate = new BarcodeCandidate();
        candidate.Format = evidence.Variant == Pdf417Variant.Compact
            ? BarcodeFormat.CompactPdf417
            : BarcodeFormat.Pdf417;
        candidate.Dimension = BarcodeDimension.StackedLinear;
        candidate.Location = CopyFinderQuadrilateral(location);
        candidate.AngleDegrees = angle;
        candidate.Confidence = confidence;
        candidate.Polarity = polarity;
        candidate.MirrorMode = BarcodeMirrorMode.Normal;
        candidate.Diagnostics = new BarcodeDiagnostic[0];
        evidence.Candidate = candidate;
        return evidence;
    }

    static BarcodePoint FitObservationPoint(
        Pdf417GuardObservation[] observations,
        bool useLeft,
        double targetOffset)
    {
        double offsetMean = 0.0;
        double xMean = 0.0;
        double yMean = 0.0;
        int index = 0;
        while (index < observations.Length)
        {
            BarcodePoint point = useLeft
                ? observations[index].Left
                : observations[index].Right;
            offsetMean = offsetMean + observations[index].Offset;
            xMean = xMean + point.X;
            yMean = yMean + point.Y;
            index = index + 1;
        }

        offsetMean = offsetMean / observations.Length;
        xMean = xMean / observations.Length;
        yMean = yMean / observations.Length;
        double denominator = 0.0;
        double xNumerator = 0.0;
        double yNumerator = 0.0;
        index = 0;
        while (index < observations.Length)
        {
            BarcodePoint point = useLeft
                ? observations[index].Left
                : observations[index].Right;
            double offsetDelta = observations[index].Offset - offsetMean;
            denominator = denominator + (offsetDelta * offsetDelta);
            xNumerator = xNumerator
                + (offsetDelta * (point.X - xMean));
            yNumerator = yNumerator
                + (offsetDelta * (point.Y - yMean));
            index = index + 1;
        }

        double xSlope = denominator <= GeometryMath.Epsilon
            ? 0.0
            : xNumerator / denominator;
        double ySlope = denominator <= GeometryMath.Epsilon
            ? 0.0
            : yNumerator / denominator;
        double delta = targetOffset - offsetMean;
        return GeometryMath.CreatePoint(
            xMean + (xSlope * delta),
            yMean + (ySlope * delta));
    }

    static Pdf417FinderEvidence? MeasureCandidateQuadrilateral(
        ModuleMatrix image,
        BarcodeQuadrilateral quadrilateral,
        BarcodePolarity polarity)
    {
        if (polarity == BarcodePolarity.Automatic)
        {
            Pdf417FinderEvidence? dark = MeasureCandidateQuadrilateralExact(
                image,
                quadrilateral,
                BarcodePolarity.DarkOnLight);
            Pdf417FinderEvidence? light = MeasureCandidateQuadrilateralExact(
                image,
                quadrilateral,
                BarcodePolarity.LightOnDark);
            if (dark == null)
            {
                return light;
            }

            if (light == null || dark.Confidence >= light.Confidence)
            {
                return dark;
            }

            return light;
        }

        return MeasureCandidateQuadrilateralExact(
            image,
            quadrilateral,
            polarity);
    }

    static Pdf417FinderEvidence? MeasureCandidateQuadrilateralExact(
        ModuleMatrix image,
        BarcodeQuadrilateral quadrilateral,
        BarcodePolarity polarity)
    {
        BarcodePoint topLeft = GeometryMath.RequirePoint(quadrilateral.TopLeft);
        BarcodePoint topRight = GeometryMath.RequirePoint(quadrilateral.TopRight);
        BarcodePoint bottomRight = GeometryMath.RequirePoint(
            quadrilateral.BottomRight);
        BarcodePoint bottomLeft = GeometryMath.RequirePoint(
            quadrilateral.BottomLeft);
        double leftLength = GeometryMath.DistanceBetweenPoints(
            topLeft,
            bottomLeft);
        double rightLength = GeometryMath.DistanceBetweenPoints(
            topRight,
            bottomRight);
        int lineCount = (int)global::System.Math.Round(
            (leftLength + rightLength) * 0.5) + 1;
        if (lineCount < MinimumRowEvidenceCount)
        {
            lineCount = MinimumRowEvidenceCount;
        }

        if (lineCount > MaximumSampledLineCount)
        {
            lineCount = MaximumSampledLineCount;
        }

        global::System.Collections.Generic.List<Pdf417GuardObservation> observations =
            new global::System.Collections.Generic.List<Pdf417GuardObservation>();
        int line = 0;
        while (line < lineCount)
        {
            double fraction = lineCount == 1
                ? 0.5
                : line / (double)(lineCount - 1);
            BarcodePoint left = GeometryMath.InterpolatePoint(
                topLeft,
                bottomLeft,
                fraction);
            BarcodePoint right = GeometryMath.InterpolatePoint(
                topRight,
                bottomRight,
                fraction);
            Pdf417GuardObservation? observation = SampleCandidateLine(
                image,
                polarity,
                left,
                right,
                line);
            if (observation != null)
            {
                observations.Add(observation);
            }

            line = line + 1;
        }

        Pdf417GuardObservation[] selected = SelectLargestGuardGroup(
            observations,
            2.01);
        if (selected.Length < MinimumRowEvidenceCount)
        {
            return null;
        }

        Pdf417FinderEvidence evidence = CreateFinderEvidence(
            selected,
            polarity);
        evidence.Location = CopyFinderQuadrilateral(quadrilateral);
        evidence.Candidate.Location = CopyFinderQuadrilateral(quadrilateral);
        return evidence;
    }

    static Pdf417GuardObservation? SampleCandidateLine(
        ModuleMatrix image,
        BarcodePolarity polarity,
        BarcodePoint left,
        BarcodePoint right,
        int offset)
    {
        double length = GeometryMath.DistanceBetweenPoints(left, right);
        int sampleCount = (int)global::System.Math.Round(length) + 1;
        if (sampleCount < Pdf417CodewordTable.StartPatternModuleCount
            + Pdf417CodewordTable.StopPatternModuleCount)
        {
            return null;
        }

        BitRow row = new BitRow(sampleCount);
        int sample = 0;
        while (sample < sampleCount)
        {
            double fraction = sampleCount == 1
                ? 0.0
                : sample / (double)(sampleCount - 1);
            BarcodePoint point = GeometryMath.InterpolatePoint(
                left,
                right,
                fraction);
            int x = (int)global::System.Math.Round(point.X);
            int y = (int)global::System.Math.Round(point.Y);
            if (x >= 0 && x < image.Width && y >= 0 && y < image.Height)
            {
                bool value = image.GetModule(x, y);
                bool foreground = polarity == BarcodePolarity.DarkOnLight
                    ? value
                    : !value;
                if (foreground)
                {
                    row.SetBit(sample, true);
                }
            }

            sample = sample + 1;
        }

        Pdf417GuardPair? pair = FindGuardPair(row);
        if (pair == null)
        {
            return null;
        }

        double leftFraction = pair.StartIndex
            / (double)(sampleCount - 1);
        double rightFraction = (pair.StopEndIndex - 1)
            / (double)(sampleCount - 1);
        Pdf417GuardObservation observation = new Pdf417GuardObservation();
        observation.Offset = offset;
        observation.Left = GeometryMath.InterpolatePoint(
            left,
            right,
            leftFraction);
        observation.Right = GeometryMath.InterpolatePoint(
            left,
            right,
            rightFraction);
        observation.StartVariance = pair.StartVariance;
        observation.StopVariance = pair.StopVariance;
        observation.ModuleSize = pair.ModuleSize;
        observation.EstimatedColumnCount = pair.EstimatedColumnCount;
        observation.DimensionScore = pair.DimensionScore;
        return observation;
    }

    static double CalculateFinderPatternScore(double variance)
    {
        double score = 1.0
            - (variance / MaximumAverageGuardVariance);
        if (score < 0.0)
        {
            return 0.0;
        }

        return score;
    }

    static int SelectFinderColumnCount(int[] votes)
    {
        int selected = Pdf417SymbolPlanner.MinimumColumns;
        int selectedVotes = 0;
        int columns = Pdf417SymbolPlanner.MinimumColumns;
        while (columns <= Pdf417SymbolPlanner.MaximumColumns)
        {
            if (votes[columns] > selectedVotes)
            {
                selected = columns;
                selectedVotes = votes[columns];
            }

            columns = columns + 1;
        }

        return selected;
    }

    static bool MeetsFinderThresholds(Pdf417FinderEvidence evidence)
    {
        return evidence.RowEvidenceCount >= MinimumRowEvidenceCount
            && evidence.StartLineScore >= MinimumStartLineScore
            && evidence.StopLineScore >= MinimumStopLineScore
            && evidence.DimensionScore >= MinimumDimensionScore
            && evidence.Confidence >= MinimumConfidence;
    }

    static double NormalizeFinderAngle(double angle)
    {
        double normalized = angle;
        while (normalized < 0.0)
        {
            normalized = normalized + 360.0;
        }

        while (normalized >= 360.0)
        {
            normalized = normalized - 360.0;
        }

        return normalized;
    }

    static BarcodeQuadrilateral MoveFinderCorner(
        BarcodeQuadrilateral source,
        int corner,
        int direction)
    {
        BarcodeQuadrilateral copy = CopyFinderQuadrilateral(source);
        BarcodePoint point;
        if (corner == 0)
        {
            point = GeometryMath.RequirePoint(copy.TopLeft);
        }
        else if (corner == 1)
        {
            point = GeometryMath.RequirePoint(copy.TopRight);
        }
        else if (corner == 2)
        {
            point = GeometryMath.RequirePoint(copy.BottomRight);
        }
        else
        {
            point = GeometryMath.RequirePoint(copy.BottomLeft);
        }

        int[] deltaX = new int[] { -1, 0, 1, -1, 1, -1, 0, 1 };
        int[] deltaY = new int[] { -1, -1, -1, 0, 0, 1, 1, 1 };
        point.X = point.X + deltaX[direction];
        point.Y = point.Y + deltaY[direction];
        copy.Bounds = GeometryMath.CalculateQuadrilateralBounds(copy);
        return copy;
    }

    static bool IsUsableFinderQuadrilateral(
        BarcodeQuadrilateral quadrilateral)
    {
        return GeometryMath.CalculateQuadrilateralSignedArea(quadrilateral)
            > 1.0;
    }

    static BarcodeQuadrilateral CopyFinderQuadrilateral(
        BarcodeQuadrilateral source)
    {
        return GeometryMath.CreateQuadrilateral(
            GeometryMath.RequirePoint(source.TopLeft),
            GeometryMath.RequirePoint(source.TopRight),
            GeometryMath.RequirePoint(source.BottomRight),
            GeometryMath.RequirePoint(source.BottomLeft));
    }

    static void ValidateFinderInput(
        ModuleMatrix image,
        BarcodePolarity polarity)
    {
        if (image == null)
        {
            throw new BarcodeValidationException("PDF417 finder image must not be null.");
        }

        if (image.Width < MinimumImageDimension
            || image.Height < MinimumImageDimension)
        {
            throw new BarcodeValidationException("PDF417 finder image dimensions are too small.");
        }

        if (polarity != BarcodePolarity.DarkOnLight
            && polarity != BarcodePolarity.LightOnDark
            && polarity != BarcodePolarity.Automatic)
        {
            throw new BarcodeValidationException("PDF417 finder polarity is invalid.");
        }
    }
}

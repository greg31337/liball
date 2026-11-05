namespace barcode;

internal class DataMatrixFinderEvidence
{
    internal BarcodeCandidate Candidate = new BarcodeCandidate();
    internal BarcodeQuadrilateral Location = new BarcodeQuadrilateral();
    internal DataMatrixSymbolInfo SymbolInfo = new DataMatrixSymbolInfo();
    internal BarcodeRotation Rotation;
    internal BarcodePolarity Polarity;
    internal BarcodeMirrorMode MirrorMode;
    internal double SolidBorderScore;
    internal double ClockTrackScore;
    internal double DimensionScore;
    internal double ModuleWidth;
    internal double ModuleHeight;
    internal double Confidence;
}

internal class DataMatrixFinderRun
{
    internal int FixedCoordinate;
    internal int Start;
    internal int End;
}

internal static class DataMatrixImageFinder
{
    internal const int MinimumFinderPixels = 8;
    internal const int MaximumRunCount = 4096;
    internal const int MaximumCandidateCount = 64;
    internal const int MaximumRefinementAdjustment = 32;
    internal const double MinimumSolidBorderScore = 0.90;
    internal const double MinimumClockTrackScore = 0.70;
    internal const double MinimumDimensionScore = 0.65;
    internal const double MinimumConfidence = 0.82;
    internal const double MaximumForegroundFraction = 0.80;

    internal static DataMatrixFinderEvidence[] Locate(
        ModuleMatrix modules,
        BarcodePolarity polarity)
    {
        ValidateImageAndPolarity(modules, polarity);
        global::System.Collections.Generic.List<DataMatrixFinderEvidence> evidence =
            new global::System.Collections.Generic.List<DataMatrixFinderEvidence>();
        if (polarity == BarcodePolarity.Automatic)
        {
            LocateForPolarity(modules, BarcodePolarity.DarkOnLight, evidence);
            LocateForPolarity(modules, BarcodePolarity.LightOnDark, evidence);
        }
        else
        {
            LocateForPolarity(modules, polarity, evidence);
        }

        SortEvidence(evidence);
        return evidence.ToArray();
    }

    internal static DataMatrixFinderEvidence? EvaluateQuadrilateral(
        ModuleMatrix modules,
        BarcodeQuadrilateral candidate,
        BarcodePolarity polarity)
    {
        ValidateImageAndPolarity(modules, polarity);
        GeometryMath.ValidateQuadrilateral(candidate);
        DataMatrixFinderEvidence? evidence = FindBestEvidence(modules, candidate, polarity);
        if (evidence == null || !MeetsAcceptanceThresholds(evidence))
        {
            return null;
        }

        return evidence;
    }

    internal static DataMatrixFinderEvidence MeasureQuadrilateral(
        ModuleMatrix modules,
        BarcodeQuadrilateral candidate,
        BarcodePolarity polarity)
    {
        ValidateImageAndPolarity(modules, polarity);
        GeometryMath.ValidateQuadrilateral(candidate);
        DataMatrixFinderEvidence? evidence = FindBestEvidence(modules, candidate, polarity);
        if (evidence == null)
        {
            throw new BarcodeDetectionException("Data Matrix finder evidence could not be measured.");
        }

        return evidence;
    }

    internal static DataMatrixFinderEvidence? RefineQuadrilateral(
        ModuleMatrix modules,
        BarcodeQuadrilateral estimate,
        BarcodePolarity polarity,
        int maximumAdjustment)
    {
        ValidateImageAndPolarity(modules, polarity);
        GeometryMath.ValidateQuadrilateral(estimate);
        if (maximumAdjustment < 0 || maximumAdjustment > MaximumRefinementAdjustment)
        {
            throw new BarcodeValidationException("Data Matrix finder refinement adjustment is outside the supported range.");
        }

        BarcodeQuadrilateral current = CopyQuadrilateral(estimate);
        DataMatrixFinderEvidence? best = FindBestEvidence(modules, current, polarity);
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
                    BarcodeQuadrilateral adjusted = MoveCorner(current, corner, direction);
                    if (IsUsableQuadrilateral(adjusted))
                    {
                        DataMatrixFinderEvidence? candidate = ScoreCandidate(
                            modules,
                            adjusted,
                            best == null ? ResolveRefinementPolarity(polarity) : best.Polarity,
                            best == null ? BarcodeRotation.None : best.Rotation,
                            best == null ? BarcodeMirrorMode.Normal : best.MirrorMode,
                            best == null
                                ? DataMatrixSymbolInfoTable.GetSymbolByVersion(1)
                                : best.SymbolInfo);
                        if (candidate != null
                            && (best == null || candidate.Confidence > best.Confidence + 0.000001))
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

        if (best == null || !MeetsAcceptanceThresholds(best))
        {
            return null;
        }

        return best;
    }

    static void LocateForPolarity(
        ModuleMatrix modules,
        BarcodePolarity polarity,
        global::System.Collections.Generic.List<DataMatrixFinderEvidence> evidence)
    {
        int foregroundCount = CountForegroundModules(modules, polarity);
        double foregroundFraction = (double)foregroundCount
            / (double)(modules.Width * modules.Height);
        if (foregroundFraction > MaximumForegroundFraction)
        {
            return;
        }

        global::System.Collections.Generic.List<DataMatrixFinderRun> horizontal =
            FindHorizontalRuns(modules, polarity);
        global::System.Collections.Generic.List<DataMatrixFinderRun> vertical =
            FindVerticalRuns(modules, polarity);
        int initialCount = evidence.Count;
        AddRunPairCandidates(modules, polarity, horizontal, vertical, evidence);
        if (evidence.Count == initialCount)
        {
            AddComponentCandidates(modules, polarity, evidence);
        }
    }

    static global::System.Collections.Generic.List<DataMatrixFinderRun> FindHorizontalRuns(
        ModuleMatrix modules,
        BarcodePolarity polarity)
    {
        global::System.Collections.Generic.List<DataMatrixFinderRun> runs =
            new global::System.Collections.Generic.List<DataMatrixFinderRun>();
        int row = 0;
        while (row < modules.Height && runs.Count < MaximumRunCount)
        {
            int column = 0;
            while (column < modules.Width && runs.Count < MaximumRunCount)
            {
                while (column < modules.Width && !IsForeground(modules, column, row, polarity))
                {
                    column = column + 1;
                }

                int start = column;
                while (column < modules.Width && IsForeground(modules, column, row, polarity))
                {
                    column = column + 1;
                }

                if (column - start >= MinimumFinderPixels)
                {
                    DataMatrixFinderRun run = new DataMatrixFinderRun();
                    run.FixedCoordinate = row;
                    run.Start = start;
                    run.End = column - 1;
                    runs.Add(run);
                }
            }

            row = row + 1;
        }

        return runs;
    }

    static global::System.Collections.Generic.List<DataMatrixFinderRun> FindVerticalRuns(
        ModuleMatrix modules,
        BarcodePolarity polarity)
    {
        global::System.Collections.Generic.List<DataMatrixFinderRun> runs =
            new global::System.Collections.Generic.List<DataMatrixFinderRun>();
        int column = 0;
        while (column < modules.Width && runs.Count < MaximumRunCount)
        {
            int row = 0;
            while (row < modules.Height && runs.Count < MaximumRunCount)
            {
                while (row < modules.Height && !IsForeground(modules, column, row, polarity))
                {
                    row = row + 1;
                }

                int start = row;
                while (row < modules.Height && IsForeground(modules, column, row, polarity))
                {
                    row = row + 1;
                }

                if (row - start >= MinimumFinderPixels)
                {
                    DataMatrixFinderRun run = new DataMatrixFinderRun();
                    run.FixedCoordinate = column;
                    run.Start = start;
                    run.End = row - 1;
                    runs.Add(run);
                }
            }

            column = column + 1;
        }

        return runs;
    }

    static void AddRunPairCandidates(
        ModuleMatrix modules,
        BarcodePolarity polarity,
        global::System.Collections.Generic.List<DataMatrixFinderRun> horizontal,
        global::System.Collections.Generic.List<DataMatrixFinderRun> vertical,
        global::System.Collections.Generic.List<DataMatrixFinderEvidence> evidence)
    {
        global::System.Collections.Generic.List<int[]> triedBounds =
            new global::System.Collections.Generic.List<int[]>();
        int horizontalIndex = 0;
        int attemptCount = 0;
        while (horizontalIndex < horizontal.Count && attemptCount < MaximumRunCount * 2)
        {
            DataMatrixFinderRun horizontalRun = horizontal[horizontalIndex];
            int verticalIndex = 0;
            while (verticalIndex < vertical.Count && attemptCount < MaximumRunCount * 2)
            {
                DataMatrixFinderRun verticalRun = vertical[verticalIndex];
                int horizontalLength = horizontalRun.End - horizontalRun.Start + 1;
                int verticalLength = verticalRun.End - verticalRun.Start + 1;
                int minimumLength = horizontalLength < verticalLength
                    ? horizontalLength
                    : verticalLength;
                int tolerance = minimumLength / 6;
                if (tolerance < 1)
                {
                    tolerance = 1;
                }

                if (EndpointsMeet(horizontalRun, verticalRun, tolerance))
                {
                    int[] bounds = new int[]
                    {
                        horizontalRun.Start,
                        verticalRun.Start,
                        horizontalRun.End,
                        verticalRun.End
                    };
                    if (!ContainsBounds(triedBounds, bounds))
                    {
                        triedBounds.Add(bounds);
                        BarcodeQuadrilateral seed = CreateAxisAlignedQuadrilateral(
                            bounds[0],
                            bounds[1],
                            bounds[2],
                            bounds[3]);
                        DataMatrixFinderEvidence? candidate = EvaluateQuadrilateral(
                            modules,
                            seed,
                            polarity);
                        if (candidate != null)
                        {
                            AddDistinctEvidence(evidence, candidate);
                        }
                    }

                    attemptCount = attemptCount + 1;
                }

                verticalIndex = verticalIndex + 1;
            }

            horizontalIndex = horizontalIndex + 1;
        }
    }

    static bool ContainsBounds(
        global::System.Collections.Generic.List<int[]> bounds,
        int[] candidate)
    {
        int index = 0;
        while (index < bounds.Count)
        {
            int[] existing = bounds[index];
            if (existing[0] == candidate[0]
                && existing[1] == candidate[1]
                && existing[2] == candidate[2]
                && existing[3] == candidate[3])
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool EndpointsMeet(
        DataMatrixFinderRun horizontal,
        DataMatrixFinderRun vertical,
        int tolerance)
    {
        bool xMatches = global::System.Math.Abs(vertical.FixedCoordinate - horizontal.Start) <= tolerance
            || global::System.Math.Abs(vertical.FixedCoordinate - horizontal.End) <= tolerance;
        bool yMatches = global::System.Math.Abs(horizontal.FixedCoordinate - vertical.Start) <= tolerance
            || global::System.Math.Abs(horizontal.FixedCoordinate - vertical.End) <= tolerance;
        return xMatches && yMatches;
    }

    static void AddComponentCandidates(
        ModuleMatrix modules,
        BarcodePolarity polarity,
        global::System.Collections.Generic.List<DataMatrixFinderEvidence> evidence)
    {
        bool[] visited = new bool[modules.Width * modules.Height];
        int row = 0;
        while (row < modules.Height)
        {
            int column = 0;
            while (column < modules.Width)
            {
                int index = (row * modules.Width) + column;
                if (!visited[index] && IsForeground(modules, column, row, polarity))
                {
                    global::System.Collections.Generic.List<int> component = CollectComponent(
                        modules,
                        polarity,
                        column,
                        row,
                        visited);
                    if (component.Count >= MinimumFinderPixels * 2
                        && component.Count < ((long)modules.Width * (long)modules.Height * 3L) / 4L)
                    {
                        AddComponentSeedCandidates(modules, polarity, component, evidence);
                    }
                }

                column = column + 1;
            }

            row = row + 1;
        }
    }

    static global::System.Collections.Generic.List<int> CollectComponent(
        ModuleMatrix modules,
        BarcodePolarity polarity,
        int startColumn,
        int startRow,
        bool[] visited)
    {
        global::System.Collections.Generic.Queue<int> queue =
            new global::System.Collections.Generic.Queue<int>();
        global::System.Collections.Generic.List<int> component =
            new global::System.Collections.Generic.List<int>();
        int start = (startRow * modules.Width) + startColumn;
        visited[start] = true;
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            int index = queue.Dequeue();
            component.Add(index);
            int column = index % modules.Width;
            int row = index / modules.Width;
            AddComponentNeighbour(modules, polarity, column - 1, row, visited, queue);
            AddComponentNeighbour(modules, polarity, column + 1, row, visited, queue);
            AddComponentNeighbour(modules, polarity, column, row - 1, visited, queue);
            AddComponentNeighbour(modules, polarity, column, row + 1, visited, queue);
        }

        return component;
    }

    static void AddComponentNeighbour(
        ModuleMatrix modules,
        BarcodePolarity polarity,
        int column,
        int row,
        bool[] visited,
        global::System.Collections.Generic.Queue<int> queue)
    {
        if (column < 0 || column >= modules.Width || row < 0 || row >= modules.Height)
        {
            return;
        }

        int index = (row * modules.Width) + column;
        if (!visited[index] && IsForeground(modules, column, row, polarity))
        {
            visited[index] = true;
            queue.Enqueue(index);
        }
    }

    static void AddComponentSeedCandidates(
        ModuleMatrix modules,
        BarcodePolarity polarity,
        global::System.Collections.Generic.List<int> component,
        global::System.Collections.Generic.List<DataMatrixFinderEvidence> evidence)
    {
        BarcodePoint first = CreateComponentPoint(component[0], modules.Width);
        BarcodePoint endpointA = FindFarthestComponentPoint(component, modules.Width, first);
        BarcodePoint endpointB = FindFarthestComponentPoint(component, modules.Width, endpointA);
        double length = GeometryMath.DistanceBetweenPoints(endpointA, endpointB);
        if (length < MinimumFinderPixels)
        {
            return;
        }

        BarcodePoint? positive = null;
        BarcodePoint? negative = null;
        double maximumPositive = 0.0;
        double maximumNegative = 0.0;
        int index = 0;
        while (index < component.Count)
        {
            BarcodePoint point = CreateComponentPoint(component[index], modules.Width);
            double cross = GeometryMath.CalculateCrossProduct(endpointA, endpointB, point) / length;
            if (cross > maximumPositive)
            {
                maximumPositive = cross;
                positive = point;
            }

            if (-cross > maximumNegative)
            {
                maximumNegative = -cross;
                negative = point;
            }

            index = index + 1;
        }

        double minimumDistance = length * 0.12;
        if (positive != null && maximumPositive >= minimumDistance)
        {
            AddInferredComponentCandidate(
                modules,
                polarity,
                endpointA,
                endpointB,
                positive,
                evidence);
        }

        if (negative != null && maximumNegative >= minimumDistance)
        {
            AddInferredComponentCandidate(
                modules,
                polarity,
                endpointA,
                endpointB,
                negative,
                evidence);
        }
    }

    static BarcodePoint FindFarthestComponentPoint(
        global::System.Collections.Generic.List<int> component,
        int width,
        BarcodePoint origin)
    {
        BarcodePoint farthest = CreateComponentPoint(component[0], width);
        double maximumDistance = -1.0;
        int index = 0;
        while (index < component.Count)
        {
            BarcodePoint point = CreateComponentPoint(component[index], width);
            double deltaX = point.X - origin.X;
            double deltaY = point.Y - origin.Y;
            double distance = (deltaX * deltaX) + (deltaY * deltaY);
            if (distance > maximumDistance)
            {
                maximumDistance = distance;
                farthest = point;
            }

            index = index + 1;
        }

        return farthest;
    }

    static BarcodePoint CreateComponentPoint(int index, int width)
    {
        return GeometryMath.CreatePoint(index % width, index / width);
    }

    static void AddInferredComponentCandidate(
        ModuleMatrix modules,
        BarcodePolarity polarity,
        BarcodePoint endpointA,
        BarcodePoint endpointB,
        BarcodePoint shared,
        global::System.Collections.Generic.List<DataMatrixFinderEvidence> evidence)
    {
        BarcodePoint inferred = GeometryMath.CreatePoint(
            endpointA.X + endpointB.X - shared.X,
            endpointA.Y + endpointB.Y - shared.Y);
        BarcodeQuadrilateral? seed = OrderQuadrilateral(
            endpointA,
            endpointB,
            shared,
            inferred);
        if (seed == null || !IsUsableQuadrilateral(seed))
        {
            return;
        }

        DataMatrixFinderEvidence? candidate = RefineQuadrilateral(
            modules,
            seed,
            polarity,
            4);
        if (candidate != null)
        {
            AddDistinctEvidence(evidence, candidate);
        }
    }

    static DataMatrixFinderEvidence? FindBestEvidence(
        ModuleMatrix modules,
        BarcodeQuadrilateral candidate,
        BarcodePolarity polarity)
    {
        if (polarity == BarcodePolarity.Automatic)
        {
            DataMatrixFinderEvidence? dark = FindBestForExactPolarity(
                modules,
                candidate,
                BarcodePolarity.DarkOnLight);
            DataMatrixFinderEvidence? light = FindBestForExactPolarity(
                modules,
                candidate,
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

        return FindBestForExactPolarity(modules, candidate, polarity);
    }

    static DataMatrixFinderEvidence? FindBestForExactPolarity(
        ModuleMatrix modules,
        BarcodeQuadrilateral candidate,
        BarcodePolarity polarity)
    {
        DataMatrixFinderEvidence? best = null;
        int mirrorValue = 0;
        while (mirrorValue < 2)
        {
            BarcodeMirrorMode mirror = (BarcodeMirrorMode)mirrorValue;
            int rotationValue = 0;
            while (rotationValue < 4)
            {
                BarcodeRotation rotation = (BarcodeRotation)rotationValue;
                int version = 1;
                while (version <= DataMatrixSymbolInfoTable.SymbolCount)
                {
                    DataMatrixSymbolInfo symbol = DataMatrixSymbolInfoTable.GetSymbolByVersion(version);
                    DataMatrixFinderEvidence evidence = ScoreCandidate(
                        modules,
                        candidate,
                        polarity,
                        rotation,
                        mirror,
                        symbol);
                    if (best == null || evidence.Confidence > best.Confidence)
                    {
                        best = evidence;
                    }

                    version = version + 1;
                }

                rotationValue = rotationValue + 1;
            }

            mirrorValue = mirrorValue + 1;
        }

        return best;
    }

    static DataMatrixFinderEvidence ScoreCandidate(
        ModuleMatrix modules,
        BarcodeQuadrilateral candidate,
        BarcodePolarity polarity,
        BarcodeRotation rotation,
        BarcodeMirrorMode mirror,
        DataMatrixSymbolInfo symbol)
    {
        BarcodePoint topLeft = GeometryMath.RequirePoint(candidate.TopLeft);
        BarcodePoint topRight = GeometryMath.RequirePoint(candidate.TopRight);
        BarcodePoint bottomRight = GeometryMath.RequirePoint(candidate.BottomRight);
        BarcodePoint bottomLeft = GeometryMath.RequirePoint(candidate.BottomLeft);
        int observedColumns = rotation == BarcodeRotation.None
            || rotation == BarcodeRotation.Clockwise180
            ? symbol.SymbolColumns
            : symbol.SymbolRows;
        int observedRows = rotation == BarcodeRotation.None
            || rotation == BarcodeRotation.Clockwise180
            ? symbol.SymbolRows
            : symbol.SymbolColumns;
        double horizontalLength = (GeometryMath.DistanceBetweenPoints(topLeft, topRight)
            + GeometryMath.DistanceBetweenPoints(bottomLeft, bottomRight)) * 0.5;
        double verticalLength = (GeometryMath.DistanceBetweenPoints(topLeft, bottomLeft)
            + GeometryMath.DistanceBetweenPoints(topRight, bottomRight)) * 0.5;
        double moduleWidth = (horizontalLength + 1.0) / (double)observedColumns;
        double moduleHeight = (verticalLength + 1.0) / (double)observedRows;
        double dimensionScore = CalculateDimensionScore(moduleWidth, moduleHeight);
        double rowInset = 0.5 / (double)observedRows;
        double columnInset = 0.5 / (double)observedColumns;
        BarcodePoint topStart = GeometryMath.InterpolatePoint(topLeft, bottomLeft, rowInset);
        BarcodePoint topEnd = GeometryMath.InterpolatePoint(topRight, bottomRight, rowInset);
        BarcodePoint rightStart = GeometryMath.InterpolatePoint(topRight, topLeft, columnInset);
        BarcodePoint rightEnd = GeometryMath.InterpolatePoint(bottomRight, bottomLeft, columnInset);
        BarcodePoint bottomStart = GeometryMath.InterpolatePoint(bottomLeft, topLeft, rowInset);
        BarcodePoint bottomEnd = GeometryMath.InterpolatePoint(bottomRight, topRight, rowInset);
        BarcodePoint leftStart = GeometryMath.InterpolatePoint(topLeft, topRight, columnInset);
        BarcodePoint leftEnd = GeometryMath.InterpolatePoint(bottomLeft, bottomRight, columnInset);
        double topClock = SampleClockEdge(
            modules,
            topStart,
            topEnd,
            observedColumns,
            polarity);
        double rightClock = SampleClockEdge(
            modules,
            rightStart,
            rightEnd,
            observedRows,
            polarity);
        double bottomSolid = SampleSolidEdge(
            modules,
            bottomStart,
            bottomEnd,
            observedColumns,
            polarity);
        double leftSolid = SampleSolidEdge(
            modules,
            leftStart,
            leftEnd,
            observedRows,
            polarity);
        double topSolid = SampleSolidEdge(
            modules,
            topStart,
            topEnd,
            observedColumns,
            polarity);
        double rightSolid = SampleSolidEdge(
            modules,
            rightStart,
            rightEnd,
            observedRows,
            polarity);
        double bottomClock = SampleClockEdge(
            modules,
            bottomStart,
            bottomEnd,
            observedColumns,
            polarity);
        double leftClock = SampleClockEdge(
            modules,
            leftStart,
            leftEnd,
            observedRows,
            polarity);
        double solidScore;
        double clockScore;
        if (mirror == BarcodeMirrorMode.Normal && rotation == BarcodeRotation.None)
        {
            solidScore = (leftSolid + bottomSolid) * 0.5;
            clockScore = (topClock + rightClock) * 0.5;
        }
        else if (mirror == BarcodeMirrorMode.Normal
            && rotation == BarcodeRotation.Clockwise90)
        {
            solidScore = (topSolid + leftSolid) * 0.5;
            clockScore = (rightClock + bottomClock) * 0.5;
        }
        else if (mirror == BarcodeMirrorMode.Normal
            && rotation == BarcodeRotation.Clockwise180)
        {
            solidScore = (topSolid + rightSolid) * 0.5;
            clockScore = (bottomClock + leftClock) * 0.5;
        }
        else if (mirror == BarcodeMirrorMode.Normal)
        {
            solidScore = (rightSolid + bottomSolid) * 0.5;
            clockScore = (topClock + leftClock) * 0.5;
        }
        else if (rotation == BarcodeRotation.None)
        {
            solidScore = (rightSolid + bottomSolid) * 0.5;
            clockScore = (topClock + leftClock) * 0.5;
        }
        else if (rotation == BarcodeRotation.Clockwise90)
        {
            solidScore = (leftSolid + bottomSolid) * 0.5;
            clockScore = (topClock + rightClock) * 0.5;
        }
        else if (rotation == BarcodeRotation.Clockwise180)
        {
            solidScore = (topSolid + leftSolid) * 0.5;
            clockScore = (rightClock + bottomClock) * 0.5;
        }
        else
        {
            solidScore = (topSolid + rightSolid) * 0.5;
            clockScore = (bottomClock + leftClock) * 0.5;
        }

        double confidence = (solidScore * 0.45)
            + (clockScore * 0.45)
            + (dimensionScore * 0.10);
        DataMatrixFinderEvidence evidence = new DataMatrixFinderEvidence();
        evidence.Location = CopyQuadrilateral(candidate);
        evidence.SymbolInfo = symbol.CopySymbolInfo();
        evidence.Rotation = rotation;
        evidence.Polarity = polarity;
        evidence.MirrorMode = mirror;
        evidence.SolidBorderScore = solidScore;
        evidence.ClockTrackScore = clockScore;
        evidence.DimensionScore = dimensionScore;
        evidence.ModuleWidth = moduleWidth;
        evidence.ModuleHeight = moduleHeight;
        evidence.Confidence = confidence;
        evidence.Candidate = CreatePublicCandidate(evidence);
        return evidence;
    }

    static double SampleSolidEdge(
        ModuleMatrix modules,
        BarcodePoint start,
        BarcodePoint end,
        int moduleCount,
        BarcodePolarity polarity)
    {
        int sampleCount = moduleCount * 2;
        int matched = 0;
        int index = 0;
        while (index < sampleCount)
        {
            double fraction = ((double)index + 0.5) / (double)sampleCount;
            BarcodePoint point = GeometryMath.InterpolatePoint(start, end, fraction);
            if (SampleForeground(modules, point, polarity))
            {
                matched = matched + 1;
            }

            index = index + 1;
        }

        return (double)matched / (double)sampleCount;
    }

    static double SampleClockEdge(
        ModuleMatrix modules,
        BarcodePoint start,
        BarcodePoint end,
        int moduleCount,
        BarcodePolarity polarity)
    {
        int firstPhaseMatches = 0;
        int secondPhaseMatches = 0;
        int index = 0;
        while (index < moduleCount)
        {
            double fraction = ((double)index + 0.5) / (double)moduleCount;
            BarcodePoint point = GeometryMath.InterpolatePoint(start, end, fraction);
            bool foreground = SampleForeground(modules, point, polarity);
            bool firstExpected = index % 2 == 0;
            if (foreground == firstExpected)
            {
                firstPhaseMatches = firstPhaseMatches + 1;
            }

            if (foreground != firstExpected)
            {
                secondPhaseMatches = secondPhaseMatches + 1;
            }

            index = index + 1;
        }

        int matches = firstPhaseMatches > secondPhaseMatches
            ? firstPhaseMatches
            : secondPhaseMatches;
        return (double)matches / (double)moduleCount;
    }

    static bool SampleForeground(
        ModuleMatrix modules,
        BarcodePoint point,
        BarcodePolarity polarity)
    {
        int column = (int)global::System.Math.Round(point.X);
        int row = (int)global::System.Math.Round(point.Y);
        if (column < 0 || column >= modules.Width || row < 0 || row >= modules.Height)
        {
            return false;
        }

        return IsForeground(modules, column, row, polarity);
    }

    static bool IsForeground(
        ModuleMatrix modules,
        int column,
        int row,
        BarcodePolarity polarity)
    {
        bool value = modules.GetModule(column, row);
        return polarity == BarcodePolarity.LightOnDark ? !value : value;
    }

    static int CountForegroundModules(ModuleMatrix modules, BarcodePolarity polarity)
    {
        int count = 0;
        int row = 0;
        while (row < modules.Height)
        {
            int column = 0;
            while (column < modules.Width)
            {
                if (IsForeground(modules, column, row, polarity))
                {
                    count = count + 1;
                }

                column = column + 1;
            }

            row = row + 1;
        }

        return count;
    }

    static double CalculateDimensionScore(double moduleWidth, double moduleHeight)
    {
        if (moduleWidth < 0.75 || moduleHeight < 0.75)
        {
            return 0.0;
        }

        double maximum = moduleWidth > moduleHeight ? moduleWidth : moduleHeight;
        double difference = global::System.Math.Abs(moduleWidth - moduleHeight);
        double score = 1.0 - (difference / maximum);
        return score < 0.0 ? 0.0 : score;
    }

    static bool MeetsAcceptanceThresholds(DataMatrixFinderEvidence evidence)
    {
        return evidence.SolidBorderScore >= MinimumSolidBorderScore
            && evidence.ClockTrackScore >= MinimumClockTrackScore
            && evidence.DimensionScore >= MinimumDimensionScore
            && evidence.Confidence >= MinimumConfidence;
    }

    static BarcodeCandidate CreatePublicCandidate(DataMatrixFinderEvidence evidence)
    {
        BarcodeCandidate candidate = new BarcodeCandidate();
        candidate.Format = BarcodeFormat.DataMatrix;
        candidate.Dimension = BarcodeDimension.Matrix;
        candidate.Location = CopyQuadrilateral(evidence.Location);
        candidate.AngleDegrees = RotationDegrees(evidence.Rotation);
        candidate.Confidence = evidence.Confidence;
        candidate.Polarity = evidence.Polarity;
        candidate.MirrorMode = evidence.MirrorMode;
        candidate.Diagnostics = new BarcodeDiagnostic[0];
        return candidate;
    }

    static double RotationDegrees(BarcodeRotation rotation)
    {
        switch (rotation)
        {
            case BarcodeRotation.None:
                return 0.0;
            case BarcodeRotation.Clockwise90:
                return 90.0;
            case BarcodeRotation.Clockwise180:
                return 180.0;
            default:
                return 270.0;
        }
    }

    static BarcodePolarity ResolveRefinementPolarity(BarcodePolarity polarity)
    {
        return polarity == BarcodePolarity.Automatic
            ? BarcodePolarity.DarkOnLight
            : polarity;
    }

    static BarcodeQuadrilateral CreateAxisAlignedQuadrilateral(
        int left,
        int top,
        int right,
        int bottom)
    {
        return GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(left, top),
            GeometryMath.CreatePoint(right, top),
            GeometryMath.CreatePoint(right, bottom),
            GeometryMath.CreatePoint(left, bottom));
    }

    static BarcodeQuadrilateral? OrderQuadrilateral(
        BarcodePoint first,
        BarcodePoint second,
        BarcodePoint third,
        BarcodePoint fourth)
    {
        BarcodePoint[] points = new BarcodePoint[] { first, second, third, fourth };
        int topLeftIndex = FindMinimumSumIndex(points);
        int bottomRightIndex = FindMaximumSumIndex(points);
        int topRightIndex = FindMaximumDifferenceIndex(points);
        int bottomLeftIndex = FindMinimumDifferenceIndex(points);
        if (topLeftIndex == topRightIndex
            || topLeftIndex == bottomRightIndex
            || topLeftIndex == bottomLeftIndex
            || topRightIndex == bottomRightIndex
            || topRightIndex == bottomLeftIndex
            || bottomRightIndex == bottomLeftIndex)
        {
            return null;
        }

        return GeometryMath.CreateQuadrilateral(
            points[topLeftIndex],
            points[topRightIndex],
            points[bottomRightIndex],
            points[bottomLeftIndex]);
    }

    static int FindMinimumSumIndex(BarcodePoint[] points)
    {
        int selected = 0;
        double value = points[0].X + points[0].Y;
        int index = 1;
        while (index < points.Length)
        {
            double candidate = points[index].X + points[index].Y;
            if (candidate < value)
            {
                selected = index;
                value = candidate;
            }

            index = index + 1;
        }

        return selected;
    }

    static int FindMaximumSumIndex(BarcodePoint[] points)
    {
        int selected = 0;
        double value = points[0].X + points[0].Y;
        int index = 1;
        while (index < points.Length)
        {
            double candidate = points[index].X + points[index].Y;
            if (candidate > value)
            {
                selected = index;
                value = candidate;
            }

            index = index + 1;
        }

        return selected;
    }

    static int FindMaximumDifferenceIndex(BarcodePoint[] points)
    {
        int selected = 0;
        double value = points[0].X - points[0].Y;
        int index = 1;
        while (index < points.Length)
        {
            double candidate = points[index].X - points[index].Y;
            if (candidate > value)
            {
                selected = index;
                value = candidate;
            }

            index = index + 1;
        }

        return selected;
    }

    static int FindMinimumDifferenceIndex(BarcodePoint[] points)
    {
        int selected = 0;
        double value = points[0].X - points[0].Y;
        int index = 1;
        while (index < points.Length)
        {
            double candidate = points[index].X - points[index].Y;
            if (candidate < value)
            {
                selected = index;
                value = candidate;
            }

            index = index + 1;
        }

        return selected;
    }

    static BarcodeQuadrilateral MoveCorner(
        BarcodeQuadrilateral source,
        int corner,
        int direction)
    {
        BarcodeQuadrilateral copy = CopyQuadrilateral(source);
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

    static bool IsUsableQuadrilateral(BarcodeQuadrilateral candidate)
    {
        double area = GeometryMath.CalculateQuadrilateralSignedArea(candidate);
        return area > 1.0;
    }

    static BarcodeQuadrilateral CopyQuadrilateral(BarcodeQuadrilateral source)
    {
        return GeometryMath.CreateQuadrilateral(
            GeometryMath.RequirePoint(source.TopLeft),
            GeometryMath.RequirePoint(source.TopRight),
            GeometryMath.RequirePoint(source.BottomRight),
            GeometryMath.RequirePoint(source.BottomLeft));
    }

    static void AddDistinctEvidence(
        global::System.Collections.Generic.List<DataMatrixFinderEvidence> evidence,
        DataMatrixFinderEvidence candidate)
    {
        int index = 0;
        while (index < evidence.Count)
        {
            if (LocationsMatch(evidence[index], candidate))
            {
                if (candidate.Confidence > evidence[index].Confidence)
                {
                    evidence[index] = candidate;
                }

                return;
            }

            index = index + 1;
        }

        if (evidence.Count < MaximumCandidateCount)
        {
            evidence.Add(candidate);
        }
    }

    static bool LocationsMatch(
        DataMatrixFinderEvidence first,
        DataMatrixFinderEvidence second)
    {
        BarcodeRectangle firstBounds = GeometryMath.CalculateQuadrilateralBounds(first.Location);
        BarcodeRectangle secondBounds = GeometryMath.CalculateQuadrilateralBounds(second.Location);
        double tolerance = global::System.Math.Max(
            global::System.Math.Max(first.ModuleWidth, first.ModuleHeight),
            global::System.Math.Max(second.ModuleWidth, second.ModuleHeight)) * 1.5;
        return global::System.Math.Abs(firstBounds.X - secondBounds.X) <= tolerance
            && global::System.Math.Abs(firstBounds.Y - secondBounds.Y) <= tolerance
            && global::System.Math.Abs(firstBounds.Width - secondBounds.Width) <= tolerance * 2.0
            && global::System.Math.Abs(firstBounds.Height - secondBounds.Height) <= tolerance * 2.0;
    }

    static void SortEvidence(
        global::System.Collections.Generic.List<DataMatrixFinderEvidence> evidence)
    {
        int index = 1;
        while (index < evidence.Count)
        {
            DataMatrixFinderEvidence value = evidence[index];
            int insertion = index - 1;
            while (insertion >= 0 && ComesAfter(evidence[insertion], value))
            {
                evidence[insertion + 1] = evidence[insertion];
                insertion = insertion - 1;
            }

            evidence[insertion + 1] = value;
            index = index + 1;
        }
    }

    static bool ComesAfter(DataMatrixFinderEvidence first, DataMatrixFinderEvidence second)
    {
        BarcodeRectangle firstBounds = GeometryMath.CalculateQuadrilateralBounds(first.Location);
        BarcodeRectangle secondBounds = GeometryMath.CalculateQuadrilateralBounds(second.Location);
        if (firstBounds.Y > secondBounds.Y + GeometryMath.Epsilon)
        {
            return true;
        }

        return global::System.Math.Abs(firstBounds.Y - secondBounds.Y) <= GeometryMath.Epsilon
            && firstBounds.X > secondBounds.X;
    }

    static void ValidateImageAndPolarity(ModuleMatrix modules, BarcodePolarity polarity)
    {
        if (modules == null)
        {
            throw new BarcodeValidationException("Data Matrix finder image must not be null.");
        }

        if (polarity != BarcodePolarity.DarkOnLight
            && polarity != BarcodePolarity.LightOnDark
            && polarity != BarcodePolarity.Automatic)
        {
            throw new BarcodeValidationException("Data Matrix finder polarity is invalid.");
        }
    }
}

namespace qrcode;

internal class AlignmentPatternDetector
{
    internal const double MinimumPatternConfidence = 0.8;
    internal const int MaximumSearchRadius = 32;
    internal const int SearchStageCount = 3;

    internal static AlignmentPatternSearchResult Detect(
        QrRaster raster,
        FinderGroup group,
        QrDetectionOptions options)
    {
        BinaryMorphology.ValidateRaster(raster, options);
        ValidateGroup(group);
        AlignmentPatternSearchResult unavailable = new AlignmentPatternSearchResult
        {
            RefinedBottomRight = CopyPoint(group.EstimatedBottomRight)
        };
        if (!options.AllowModel2 || !group.Model2Possible)
        {
            return unavailable;
        }

        int version = group.EstimatedVersion;
        int dimension = Model2VersionTable.GetDimension(version);
        int[] positions = AlignmentPositionTable.GetPositions(version);
        int expectedCount = positions.Length * positions.Length;
        if (positions.Length > 0)
        {
            expectedCount -= 3;
        }

        if (expectedCount == 0)
        {
            return new AlignmentPatternSearchResult
            {
                Success = true,
                Patterns = new AlignmentPatternDetection[0],
                ExpectedCount = 0,
                FoundCount = 0,
                UsedFallback = false,
                Confidence = 1.0,
                RefinedBottomRight = CopyPoint(group.EstimatedBottomRight)
            };
        }

        double columnStepX = (group.TopRight.CenterX - group.TopLeft.CenterX)
            / (dimension - 7);
        double columnStepY = (group.TopRight.CenterY - group.TopLeft.CenterY)
            / (dimension - 7);
        double rowStepX = (group.BottomLeft.CenterX - group.TopLeft.CenterX)
            / (dimension - 7);
        double rowStepY = (group.BottomLeft.CenterY - group.TopLeft.CenterY)
            / (dimension - 7);
        double columnModuleSize = global::System.Math.Sqrt(
            columnStepX * columnStepX + columnStepY * columnStepY);
        double rowModuleSize = global::System.Math.Sqrt(
            rowStepX * rowStepX + rowStepY * rowStepY);
        double searchModuleSize = columnModuleSize;
        if (rowModuleSize > searchModuleSize)
        {
            searchModuleSize = rowModuleSize;
        }

        global::System.Collections.Generic.List<AlignmentPatternDetection> patterns =
            new global::System.Collections.Generic.List<AlignmentPatternDetection>();
        int foundCount = 0;
        double confidenceSum = 0.0;
        AlignmentPatternDetection? strongestRefinement = null;
        double strongestRefinementDistance = -1.0;
        for (int rowIndex = 0; rowIndex < positions.Length; rowIndex++)
        {
            for (int columnIndex = 0; columnIndex < positions.Length; columnIndex++)
            {
                bool overlapsFinder = rowIndex == 0
                    && (columnIndex == 0 || columnIndex == positions.Length - 1)
                    || rowIndex == positions.Length - 1 && columnIndex == 0;
                if (overlapsFinder)
                {
                    continue;
                }

                int logicalRow = positions[rowIndex];
                int logicalColumn = positions[columnIndex];
                QrPoint predicted = MapLogicalCenter(
                    group,
                    logicalRow,
                    logicalColumn,
                    columnStepX,
                    columnStepY,
                    rowStepX,
                    rowStepY);
                AlignmentPatternDetection detection = SearchPattern(
                    raster,
                    predicted,
                    logicalRow,
                    logicalColumn,
                    columnStepX,
                    columnStepY,
                    rowStepX,
                    rowStepY,
                    searchModuleSize);
                patterns.Add(detection);
                confidenceSum += detection.Confidence;
                if (detection.Found)
                {
                    foundCount++;
                    double rowDistance = logicalRow - 3.0;
                    double columnDistance = logicalColumn - 3.0;
                    double refinementDistance = rowDistance * rowDistance
                        + columnDistance * columnDistance;
                    if (refinementDistance > strongestRefinementDistance)
                    {
                        strongestRefinement = detection;
                        strongestRefinementDistance = refinementDistance;
                    }
                }
            }
        }

        QrPoint refinedBottomRight = CopyPoint(group.EstimatedBottomRight);
        if (strongestRefinement != null)
        {
            refinedBottomRight.X += strongestRefinement.Center.X
                - strongestRefinement.PredictedCenter.X;
            refinedBottomRight.Y += strongestRefinement.Center.Y
                - strongestRefinement.PredictedCenter.Y;
        }

        return new AlignmentPatternSearchResult
        {
            Success = true,
            Patterns = patterns.ToArray(),
            ExpectedCount = expectedCount,
            FoundCount = foundCount,
            UsedFallback = foundCount != expectedCount,
            Confidence = confidenceSum / expectedCount,
            RefinedBottomRight = refinedBottomRight
        };
    }

    static AlignmentPatternDetection SearchPattern(
        QrRaster raster,
        QrPoint predicted,
        int logicalRow,
        int logicalColumn,
        double columnStepX,
        double columnStepY,
        double rowStepX,
        double rowStepY,
        double moduleSize)
    {
        AlignmentPatternEvidence best = new AlignmentPatternEvidence
        {
            Confidence = MeasurePattern(
                raster,
                predicted.X,
                predicted.Y,
                columnStepX,
                columnStepY,
                rowStepX,
                rowStepY)
        };
        if (best.Confidence < MinimumPatternConfidence)
        {
            for (int stage = 0; stage < SearchStageCount; stage++)
            {
                int multiplier = 1 << stage;
                int radius = (int)(moduleSize * multiplier + 0.999999);
                if (radius < 1)
                {
                    radius = 1;
                }

                if (radius > MaximumSearchRadius)
                {
                    radius = MaximumSearchRadius;
                }

                int step = radius / 4;
                if (step < 1)
                {
                    step = 1;
                }

                SearchWindow(
                    raster,
                    predicted,
                    columnStepX,
                    columnStepY,
                    rowStepX,
                    rowStepY,
                    radius,
                    step,
                    best);
                best.SearchRadius = radius;
                if (best.Confidence >= MinimumPatternConfidence)
                {
                    RefineBestOffset(
                        raster,
                        predicted,
                        columnStepX,
                        columnStepY,
                        rowStepX,
                        rowStepY,
                        step,
                        best);
                    break;
                }

                if (radius == MaximumSearchRadius)
                {
                    break;
                }
            }
        }

        bool found = best.Confidence >= MinimumPatternConfidence;
        if (found)
        {
            RefineCenterRegion(
                raster,
                predicted.X + best.ShiftX,
                predicted.Y + best.ShiftY,
                moduleSize,
                best);
        }

        return new AlignmentPatternDetection
        {
            LogicalRow = logicalRow,
            LogicalColumn = logicalColumn,
            PredictedCenter = predicted,
            Center = found
                ? new QrPoint
                {
                    X = best.CenterX,
                    Y = best.CenterY
                }
                : CopyPoint(predicted),
            Found = found,
            UsedFallback = !found,
            SearchRadius = best.SearchRadius,
            Confidence = best.Confidence
        };
    }

    static void RefineCenterRegion(
        QrRaster raster,
        double approximateX,
        double approximateY,
        double moduleSize,
        AlignmentPatternEvidence evidence)
    {
        evidence.CenterX = approximateX;
        evidence.CenterY = approximateY;
        int seedX = (int)approximateX;
        int seedY = (int)approximateY;
        if (seedX < 0
            || seedY < 0
            || seedX >= raster.Width
            || seedY >= raster.Height
            || raster.Pixels[seedY * raster.Stride + seedX] != 0)
        {
            return;
        }

        int radius = (int)(moduleSize * 1.5 + 1.0);
        if (radius < 2)
        {
            radius = 2;
        }

        if (radius > 64)
        {
            radius = 64;
        }

        int minimumX = seedX - radius;
        int minimumY = seedY - radius;
        int maximumX = seedX + radius;
        int maximumY = seedY + radius;
        if (minimumX < 0)
        {
            minimumX = 0;
        }

        if (minimumY < 0)
        {
            minimumY = 0;
        }

        if (maximumX >= raster.Width)
        {
            maximumX = raster.Width - 1;
        }

        if (maximumY >= raster.Height)
        {
            maximumY = raster.Height - 1;
        }

        int width = maximumX - minimumX + 1;
        int height = maximumY - minimumY + 1;
        int area = width * height;
        bool[] visited = new bool[area];
        int[] queueX = new int[area];
        int[] queueY = new int[area];
        int head = 0;
        int tail = 1;
        queueX[0] = seedX;
        queueY[0] = seedY;
        visited[(seedY - minimumY) * width + seedX - minimumX] = true;
        double sumX = 0.0;
        double sumY = 0.0;
        int count = 0;
        while (head < tail)
        {
            int x = queueX[head];
            int y = queueY[head];
            head++;
            sumX += x + 0.5;
            sumY += y + 0.5;
            count++;
            tail = AddDarkNeighbor(
                raster,
                x - 1,
                y,
                minimumX,
                minimumY,
                maximumX,
                maximumY,
                width,
                visited,
                queueX,
                queueY,
                tail);
            tail = AddDarkNeighbor(
                raster,
                x + 1,
                y,
                minimumX,
                minimumY,
                maximumX,
                maximumY,
                width,
                visited,
                queueX,
                queueY,
                tail);
            tail = AddDarkNeighbor(
                raster,
                x,
                y - 1,
                minimumX,
                minimumY,
                maximumX,
                maximumY,
                width,
                visited,
                queueX,
                queueY,
                tail);
            tail = AddDarkNeighbor(
                raster,
                x,
                y + 1,
                minimumX,
                minimumY,
                maximumX,
                maximumY,
                width,
                visited,
                queueX,
                queueY,
                tail);
        }

        if (count > 0)
        {
            evidence.CenterX = sumX / count;
            evidence.CenterY = sumY / count;
        }
    }

    static int AddDarkNeighbor(
        QrRaster raster,
        int x,
        int y,
        int minimumX,
        int minimumY,
        int maximumX,
        int maximumY,
        int width,
        bool[] visited,
        int[] queueX,
        int[] queueY,
        int tail)
    {
        if (x < minimumX || x > maximumX || y < minimumY || y > maximumY)
        {
            return tail;
        }

        int localIndex = (y - minimumY) * width + x - minimumX;
        if (visited[localIndex])
        {
            return tail;
        }

        visited[localIndex] = true;
        if (raster.Pixels[y * raster.Stride + x] != 0)
        {
            return tail;
        }

        queueX[tail] = x;
        queueY[tail] = y;
        return tail + 1;
    }

    static void SearchWindow(
        QrRaster raster,
        QrPoint predicted,
        double columnStepX,
        double columnStepY,
        double rowStepX,
        double rowStepY,
        int radius,
        int step,
        AlignmentPatternEvidence best)
    {
        for (int shiftY = -radius; shiftY <= radius; shiftY += step)
        {
            for (int shiftX = -radius; shiftX <= radius; shiftX += step)
            {
                ConsiderOffset(
                    raster,
                    predicted,
                    columnStepX,
                    columnStepY,
                    rowStepX,
                    rowStepY,
                    shiftX,
                    shiftY,
                    best);
            }
        }
    }

    static void RefineBestOffset(
        QrRaster raster,
        QrPoint predicted,
        double columnStepX,
        double columnStepY,
        double rowStepX,
        double rowStepY,
        int coarseStep,
        AlignmentPatternEvidence best)
    {
        int centerX = best.ShiftX;
        int centerY = best.ShiftY;
        for (int shiftY = centerY - coarseStep; shiftY <= centerY + coarseStep; shiftY++)
        {
            for (int shiftX = centerX - coarseStep; shiftX <= centerX + coarseStep; shiftX++)
            {
                ConsiderOffset(
                    raster,
                    predicted,
                    columnStepX,
                    columnStepY,
                    rowStepX,
                    rowStepY,
                    shiftX,
                    shiftY,
                    best);
            }
        }
    }

    static void ConsiderOffset(
        QrRaster raster,
        QrPoint predicted,
        double columnStepX,
        double columnStepY,
        double rowStepX,
        double rowStepY,
        int shiftX,
        int shiftY,
        AlignmentPatternEvidence best)
    {
        double confidence = MeasurePattern(
            raster,
            predicted.X + shiftX,
            predicted.Y + shiftY,
            columnStepX,
            columnStepY,
            rowStepX,
            rowStepY);
        int distance = shiftX * shiftX + shiftY * shiftY;
        int bestDistance = best.ShiftX * best.ShiftX + best.ShiftY * best.ShiftY;
        if (confidence > best.Confidence
            || confidence == best.Confidence && distance < bestDistance
            || confidence == best.Confidence
                && distance == bestDistance
                && (shiftY < best.ShiftY
                    || shiftY == best.ShiftY && shiftX < best.ShiftX))
        {
            best.ShiftX = shiftX;
            best.ShiftY = shiftY;
            best.Confidence = confidence;
        }
    }

    static double MeasurePattern(
        QrRaster raster,
        double centerX,
        double centerY,
        double columnStepX,
        double columnStepY,
        double rowStepX,
        double rowStepY)
    {
        int matches = 0;
        for (int rowOffset = -2; rowOffset <= 2; rowOffset++)
        {
            for (int columnOffset = -2; columnOffset <= 2; columnOffset++)
            {
                double x = centerX
                    + columnOffset * columnStepX
                    + rowOffset * rowStepX;
                double y = centerY
                    + columnOffset * columnStepY
                    + rowOffset * rowStepY;
                if (x < 0.0 || y < 0.0 || x >= raster.Width || y >= raster.Height)
                {
                    continue;
                }

                bool dark = raster.Pixels[(int)y * raster.Stride + (int)x] == 0;
                bool expected = rowOffset == -2
                    || rowOffset == 2
                    || columnOffset == -2
                    || columnOffset == 2
                    || rowOffset == 0 && columnOffset == 0;
                if (dark == expected)
                {
                    matches++;
                }
            }
        }

        return matches / 25.0;
    }

    static QrPoint MapLogicalCenter(
        FinderGroup group,
        int logicalRow,
        int logicalColumn,
        double columnStepX,
        double columnStepY,
        double rowStepX,
        double rowStepY)
    {
        return new QrPoint
        {
            X = group.TopLeft.CenterX
                + (logicalColumn - 3) * columnStepX
                + (logicalRow - 3) * rowStepX,
            Y = group.TopLeft.CenterY
                + (logicalColumn - 3) * columnStepY
                + (logicalRow - 3) * rowStepY
        };
    }

    static QrPoint CopyPoint(QrPoint source)
    {
        return new QrPoint { X = source.X, Y = source.Y };
    }

    static void ValidateGroup(FinderGroup group)
    {
        if (group == null
            || group.TopLeft == null
            || group.TopRight == null
            || group.BottomLeft == null
            || group.EstimatedBottomRight == null
            || !IsFinite(group.TopLeft.CenterX)
            || !IsFinite(group.TopLeft.CenterY)
            || !IsFinite(group.TopRight.CenterX)
            || !IsFinite(group.TopRight.CenterY)
            || !IsFinite(group.BottomLeft.CenterX)
            || !IsFinite(group.BottomLeft.CenterY)
            || !IsFinite(group.EstimatedBottomRight.X)
            || !IsFinite(group.EstimatedBottomRight.Y)
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

        double horizontalX = group.TopRight.CenterX - group.TopLeft.CenterX;
        double horizontalY = group.TopRight.CenterY - group.TopLeft.CenterY;
        double verticalX = group.BottomLeft.CenterX - group.TopLeft.CenterX;
        double verticalY = group.BottomLeft.CenterY - group.TopLeft.CenterY;
        if (horizontalX * horizontalX + horizontalY * horizontalY <= 0.0
            || verticalX * verticalX + verticalY * verticalY <= 0.0)
        {
            throw new QrInputException(
                "The finder group axes must have positive length.",
                "group");
        }
    }

    static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
}

namespace qrcode;

internal class RunRatioScanner
{
    internal const double MaximumRatioError = 0.75;
    internal const double MinimumModuleRatio = 0.5;
    internal const double MaximumModuleRatio = 2.0;

    internal static FinderCandidate[] Scan(
        QrRaster raster,
        int scaleToSource,
        QrDetectionOptions options)
    {
        BinaryMorphology.ValidateRaster(raster, options);
        if (scaleToSource <= 0)
        {
            throw new QrInputException("The source scale must be positive.", "scaleToSource");
        }

        if (options.MaximumCandidates <= 0)
        {
            throw new QrInputException(
                "The maximum candidate count must be positive.",
                "MaximumCandidates");
        }

        if (options.MinimumModuleSize <= 0)
        {
            throw new QrInputException(
                "The minimum module size must be positive.",
                "MinimumModuleSize");
        }

        global::System.Collections.Generic.List<FinderCandidate> candidates =
            new global::System.Collections.Generic.List<FinderCandidate>();
        ScanRows(
            raster,
            scaleToSource,
            options.MinimumModuleSize,
            options.MaximumCandidates,
            candidates);
        if (candidates.Count < options.MaximumCandidates)
        {
            ScanColumns(
                raster,
                scaleToSource,
                options.MinimumModuleSize,
                options.MaximumCandidates,
                candidates);
        }

        return candidates.ToArray();
    }

    static void ScanRows(
        QrRaster raster,
        int scaleToSource,
        int minimumModuleSize,
        int maximumCandidates,
        global::System.Collections.Generic.List<FinderCandidate> candidates)
    {
        int[] lengths = new int[5];
        int[] starts = new int[5];
        bool[] darkRuns = new bool[5];
        for (int row = 0; row < raster.Height; row++)
        {
            int runCount = 0;
            int runStart = 0;
            bool dark = raster.Pixels[row * raster.Stride] == 0;
            for (int column = 1; column <= raster.Width; column++)
            {
                bool nextDark = false;
                if (column < raster.Width)
                {
                    nextDark = raster.Pixels[row * raster.Stride + column] == 0;
                }

                if (column == raster.Width || nextDark != dark)
                {
                    runCount = PushRun(
                        column - runStart,
                        runStart,
                        dark,
                        lengths,
                        starts,
                        darkRuns,
                        runCount);
                    if (runCount == 5
                        && IsFinderColorSequence(darkRuns)
                        && TryAddCandidate(
                            raster,
                            lengths,
                            starts,
                            row,
                            false,
                            scaleToSource,
                            minimumModuleSize,
                            candidates))
                    {
                        if (candidates.Count >= maximumCandidates)
                        {
                            return;
                        }
                    }

                    runStart = column;
                    dark = nextDark;
                }
            }
        }
    }

    static void ScanColumns(
        QrRaster raster,
        int scaleToSource,
        int minimumModuleSize,
        int maximumCandidates,
        global::System.Collections.Generic.List<FinderCandidate> candidates)
    {
        int[] lengths = new int[5];
        int[] starts = new int[5];
        bool[] darkRuns = new bool[5];
        for (int column = 0; column < raster.Width; column++)
        {
            int runCount = 0;
            int runStart = 0;
            bool dark = raster.Pixels[column] == 0;
            for (int row = 1; row <= raster.Height; row++)
            {
                bool nextDark = false;
                if (row < raster.Height)
                {
                    nextDark = raster.Pixels[row * raster.Stride + column] == 0;
                }

                if (row == raster.Height || nextDark != dark)
                {
                    runCount = PushRun(
                        row - runStart,
                        runStart,
                        dark,
                        lengths,
                        starts,
                        darkRuns,
                        runCount);
                    if (runCount == 5
                        && IsFinderColorSequence(darkRuns)
                        && TryAddCandidate(
                            raster,
                            lengths,
                            starts,
                            column,
                            true,
                            scaleToSource,
                            minimumModuleSize,
                            candidates))
                    {
                        if (candidates.Count >= maximumCandidates)
                        {
                            return;
                        }
                    }

                    runStart = row;
                    dark = nextDark;
                }
            }
        }
    }

    static int PushRun(
        int length,
        int start,
        bool dark,
        int[] lengths,
        int[] starts,
        bool[] darkRuns,
        int runCount)
    {
        if (runCount < 5)
        {
            lengths[runCount] = length;
            starts[runCount] = start;
            darkRuns[runCount] = dark;
            return runCount + 1;
        }

        for (int index = 0; index < 4; index++)
        {
            lengths[index] = lengths[index + 1];
            starts[index] = starts[index + 1];
            darkRuns[index] = darkRuns[index + 1];
        }

        lengths[4] = length;
        starts[4] = start;
        darkRuns[4] = dark;
        return 5;
    }

    static bool IsFinderColorSequence(bool[] darkRuns)
    {
        return darkRuns[0]
            && !darkRuns[1]
            && darkRuns[2]
            && !darkRuns[3]
            && darkRuns[4];
    }

    static bool TryAddCandidate(
        QrRaster raster,
        int[] primaryLengths,
        int[] primaryStarts,
        int line,
        bool primaryVertical,
        int scaleToSource,
        int minimumModuleSize,
        global::System.Collections.Generic.List<FinderCandidate> candidates)
    {
        double primaryConfidence = CalculateRatioConfidence(primaryLengths);
        if (primaryConfidence < 0.0)
        {
            return false;
        }

        int total = 0;
        for (int index = 0; index < primaryLengths.Length; index++)
        {
            total += primaryLengths[index];
        }

        double primaryModule = (double)total / 7.0;
        double centerX;
        double centerY;
        if (primaryVertical)
        {
            centerX = line;
            centerY = primaryStarts[2] + primaryLengths[2] / 2.0;
        }
        else
        {
            centerX = primaryStarts[2] + primaryLengths[2] / 2.0;
            centerY = line;
        }

        int centerColumn = (int)centerX;
        int centerRow = (int)centerY;
        RunRatioCheck perpendicular;
        if (primaryVertical)
        {
            perpendicular = CheckDirection(
                raster,
                centerColumn,
                centerRow,
                1,
                0,
                primaryModule);
        }
        else
        {
            perpendicular = CheckDirection(
                raster,
                centerColumn,
                centerRow,
                0,
                1,
                primaryModule);
        }

        if (!perpendicular.Success)
        {
            return false;
        }

        RunRatioCheck correctedPrimary;
        if (primaryVertical)
        {
            correctedPrimary = CheckDirection(
                raster,
                (int)perpendicular.CenterX,
                (int)perpendicular.CenterY,
                0,
                1,
                perpendicular.ModuleSize);
        }
        else
        {
            correctedPrimary = CheckDirection(
                raster,
                (int)perpendicular.CenterX,
                (int)perpendicular.CenterY,
                1,
                0,
                perpendicular.ModuleSize);
        }

        if (!correctedPrimary.Success)
        {
            return false;
        }

        double correctedCenterX = primaryVertical
            ? perpendicular.CenterX
            : correctedPrimary.CenterX;
        double correctedCenterY = primaryVertical
            ? correctedPrimary.CenterY
            : perpendicular.CenterY;
        RunRatioCheck descendingDiagonal = CheckDirection(
            raster,
            (int)correctedCenterX,
            (int)correctedCenterY,
            1,
            1,
            primaryModule);
        if (!descendingDiagonal.Success)
        {
            return false;
        }

        RunRatioCheck ascendingDiagonal = CheckDirection(
            raster,
            (int)correctedCenterX,
            (int)correctedCenterY,
            1,
            -1,
            primaryModule);
        if (!ascendingDiagonal.Success)
        {
            return false;
        }

        double minimumModule = primaryModule;
        double maximumModule = primaryModule;
        if (perpendicular.ModuleSize < minimumModule)
        {
            minimumModule = perpendicular.ModuleSize;
        }

        if (correctedPrimary.ModuleSize < minimumModule)
        {
            minimumModule = correctedPrimary.ModuleSize;
        }

        if (perpendicular.ModuleSize > maximumModule)
        {
            maximumModule = perpendicular.ModuleSize;
        }

        if (correctedPrimary.ModuleSize > maximumModule)
        {
            maximumModule = correctedPrimary.ModuleSize;
        }

        double agreement = minimumModule / maximumModule;
        double confidence = (primaryConfidence
            + perpendicular.Confidence
            + correctedPrimary.Confidence
            + descendingDiagonal.Confidence
            + ascendingDiagonal.Confidence) / 5.0;
        confidence *= agreement;
        FinderCandidate candidate = new FinderCandidate
        {
            CenterX = correctedCenterX * scaleToSource,
            CenterY = correctedCenterY * scaleToSource,
            ModuleSize = (primaryModule
                + perpendicular.ModuleSize
                + correctedPrimary.ModuleSize) / 3.0 * scaleToSource,
            Confidence = confidence,
            ScaleToSource = scaleToSource,
            ScannedVertically = primaryVertical
        };
        if (candidate.ModuleSize < minimumModuleSize)
        {
            return false;
        }

        candidates.Add(candidate);
        return true;
    }

    static RunRatioCheck CheckDirection(
        QrRaster raster,
        int centerX,
        int centerY,
        int stepX,
        int stepY,
        double expectedModule)
    {
        RunRatioCheck failure = new RunRatioCheck();
        if (!IsInside(raster, centerX, centerY)
            || raster.Pixels[centerY * raster.Stride + centerX] != 0)
        {
            return failure;
        }

        int centralLimit = (int)(expectedModule * 6.0) + 2;
        int sideLimit = (int)(expectedModule * 3.0) + 2;
        int negativeCentral = 0;
        int x = centerX;
        int y = centerY;
        while (IsInside(raster, x, y)
            && raster.Pixels[y * raster.Stride + x] == 0)
        {
            negativeCentral++;
            if (negativeCentral > centralLimit)
            {
                return failure;
            }

            x -= stepX;
            y -= stepY;
        }

        int negativeLight = 0;
        while (IsInside(raster, x, y)
            && raster.Pixels[y * raster.Stride + x] == 255)
        {
            negativeLight++;
            if (negativeLight > sideLimit)
            {
                return failure;
            }

            x -= stepX;
            y -= stepY;
        }

        int negativeOuter = 0;
        while (IsInside(raster, x, y)
            && raster.Pixels[y * raster.Stride + x] == 0)
        {
            negativeOuter++;
            if (negativeOuter > sideLimit)
            {
                return failure;
            }

            x -= stepX;
            y -= stepY;
        }

        int positiveCentral = 0;
        x = centerX + stepX;
        y = centerY + stepY;
        while (IsInside(raster, x, y)
            && raster.Pixels[y * raster.Stride + x] == 0)
        {
            positiveCentral++;
            if (negativeCentral + positiveCentral > centralLimit)
            {
                return failure;
            }

            x += stepX;
            y += stepY;
        }

        int positiveLight = 0;
        while (IsInside(raster, x, y)
            && raster.Pixels[y * raster.Stride + x] == 255)
        {
            positiveLight++;
            if (positiveLight > sideLimit)
            {
                return failure;
            }

            x += stepX;
            y += stepY;
        }

        int positiveOuter = 0;
        while (IsInside(raster, x, y)
            && raster.Pixels[y * raster.Stride + x] == 0)
        {
            positiveOuter++;
            if (positiveOuter > sideLimit)
            {
                return failure;
            }

            x += stepX;
            y += stepY;
        }

        int[] lengths = new int[]
        {
            negativeOuter,
            negativeLight,
            negativeCentral + positiveCentral,
            positiveLight,
            positiveOuter
        };
        double confidence = CalculateRatioConfidence(lengths);
        if (confidence < 0.0)
        {
            return failure;
        }

        int total = 0;
        for (int index = 0; index < lengths.Length; index++)
        {
            total += lengths[index];
        }

        double moduleSize = (double)total / 7.0;
        if (moduleSize < expectedModule * MinimumModuleRatio
            || moduleSize > expectedModule * MaximumModuleRatio)
        {
            return failure;
        }

        int centralStartX = centerX - stepX * (negativeCentral - 1);
        int centralStartY = centerY - stepY * (negativeCentral - 1);
        int centralCount = negativeCentral + positiveCentral;
        return new RunRatioCheck
        {
            Success = true,
            CenterX = centralStartX + stepX * (centralCount / 2.0),
            CenterY = centralStartY + stepY * (centralCount / 2.0),
            ModuleSize = moduleSize,
            Confidence = confidence
        };
    }

    static double CalculateRatioConfidence(int[] lengths)
    {
        int total = 0;
        for (int index = 0; index < lengths.Length; index++)
        {
            if (lengths[index] <= 0)
            {
                return -1.0;
            }

            total += lengths[index];
        }

        if (total < 7)
        {
            return -1.0;
        }

        double moduleSize = (double)total / 7.0;
        double error = 0.0;
        for (int index = 0; index < lengths.Length; index++)
        {
            double expected = moduleSize;
            if (index == 2)
            {
                expected *= 3.0;
            }

            double difference = global::System.Math.Abs(lengths[index] - expected);
            if (difference > expected * MaximumRatioError)
            {
                return -1.0;
            }

            error += difference;
        }

        double confidence = 1.0 - error / total;
        if (confidence < 0.0)
        {
            return 0.0;
        }

        return confidence;
    }

    static bool IsInside(QrRaster raster, int x, int y)
    {
        return x >= 0 && x < raster.Width && y >= 0 && y < raster.Height;
    }
}

namespace qrcode;

internal class FinderRegionValidator
{
    internal const int MaximumRegionPixels = 1048576;

    internal static FinderCandidate[] Validate(
        QrRaster raster,
        FinderCandidate[] candidates,
        QrDetectionOptions options)
    {
        BinaryMorphology.ValidateRaster(raster, options);
        if (candidates == null)
        {
            throw new QrInputException("The finder candidates are required.", "candidates");
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

        global::System.Collections.Generic.List<FinderCandidate> validated =
            new global::System.Collections.Generic.List<FinderCandidate>();
        for (int index = 0; index < candidates.Length; index++)
        {
            FinderCandidate candidate = candidates[index];
            if (candidate == null)
            {
                throw new QrInputException(
                    "Finder candidate entries cannot be null.",
                    "candidates");
            }

            if (ValidateCandidate(raster, candidate, options.AllowCroppedQuietZone))
            {
                validated.Add(candidate);
            }
        }

        return validated.ToArray();
    }

    static bool ValidateCandidate(QrRaster raster, FinderCandidate candidate, bool allowCroppedQuietZone)
    {
        if (candidate.ScaleToSource <= 0
            || candidate.ModuleSize <= 0.0
            || candidate.CenterX < 0.0
            || candidate.CenterY < 0.0
            || !IsFinite(candidate.ModuleSize)
            || !IsFinite(candidate.CenterX)
            || !IsFinite(candidate.CenterY))
        {
            throw new QrInputException(
                "Finder candidate geometry is invalid.",
                "candidates");
        }

        double moduleSize = candidate.ModuleSize / candidate.ScaleToSource;
        int centerX = (int)(candidate.CenterX / candidate.ScaleToSource);
        int centerY = (int)(candidate.CenterY / candidate.ScaleToSource);
        if (!IsInside(raster, centerX, centerY)
            || raster.Pixels[centerY * raster.Stride + centerX] != 0)
        {
            return false;
        }

        int radius = (int)(moduleSize * 6.0) + 3;
        int minimumX = centerX - radius;
        int minimumY = centerY - radius;
        int maximumX = centerX + radius;
        int maximumY = centerY + radius;
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

        ConnectedRegion centerRegion = FloodFill(
            raster,
            centerX,
            centerY,
            minimumX,
            minimumY,
            maximumX,
            maximumY);
        if (!centerRegion.Complete || centerRegion.TouchesBoundary)
        {
            return false;
        }

        int[] outerSeed = FindOuterSeed(
            raster,
            centerX,
            centerY,
            moduleSize);
        if (outerSeed[0] < 0)
        {
            return false;
        }

        ConnectedRegion outerRegion = FloodFill(
            raster,
            outerSeed[0],
            outerSeed[1],
            minimumX,
            minimumY,
            maximumX,
            maximumY);
        if (!outerRegion.Complete || outerRegion.TouchesBoundary && (!allowCroppedQuietZone
            || outerRegion.MinimumX == minimumX && minimumX > 0
            || outerRegion.MaximumX == maximumX && maximumX < raster.Width - 1
            || outerRegion.MinimumY == minimumY && minimumY > 0
            || outerRegion.MaximumY == maximumY && maximumY < raster.Height - 1))
        {
            return false;
        }

        double regionConfidence = CalculateRegionConfidence(
            centerRegion,
            outerRegion,
            centerX,
            centerY,
            moduleSize);
        if (regionConfidence <= 0.0)
        {
            return false;
        }

        double scale = candidate.ScaleToSource;
        candidate.RegionValidated = true;
        candidate.RegionConfidence = regionConfidence;
        candidate.CenterRegionArea = centerRegion.Area;
        candidate.OuterRegionArea = outerRegion.Area;
        candidate.FinderCorners = new QrQuadrilateral
        {
            TopLeft = new QrPoint
            {
                X = outerRegion.MinimumSumX * scale,
                Y = outerRegion.MinimumSumY * scale
            },
            TopRight = new QrPoint
            {
                X = (outerRegion.MaximumDifferenceX + 1) * scale,
                Y = outerRegion.MaximumDifferenceY * scale
            },
            BottomRight = new QrPoint
            {
                X = (outerRegion.MaximumSumX + 1) * scale,
                Y = (outerRegion.MaximumSumY + 1) * scale
            },
            BottomLeft = new QrPoint
            {
                X = outerRegion.MinimumDifferenceX * scale,
                Y = (outerRegion.MinimumDifferenceY + 1) * scale
            }
        };
        candidate.Confidence = (candidate.Confidence + regionConfidence) / 2.0;
        return true;
    }

    static int[] FindOuterSeed(
        QrRaster raster,
        int centerX,
        int centerY,
        double moduleSize)
    {
        int maximumDistance = (int)(moduleSize * 5.0) + 3;
        int[] seed = SearchForOuterSeed(
            raster,
            centerX,
            centerY,
            -1,
            0,
            maximumDistance);
        if (seed[0] >= 0)
        {
            return seed;
        }

        seed = SearchForOuterSeed(
            raster,
            centerX,
            centerY,
            1,
            0,
            maximumDistance);
        if (seed[0] >= 0)
        {
            return seed;
        }

        seed = SearchForOuterSeed(
            raster,
            centerX,
            centerY,
            0,
            -1,
            maximumDistance);
        if (seed[0] >= 0)
        {
            return seed;
        }

        return SearchForOuterSeed(
            raster,
            centerX,
            centerY,
            0,
            1,
            maximumDistance);
    }

    static int[] SearchForOuterSeed(
        QrRaster raster,
        int centerX,
        int centerY,
        int stepX,
        int stepY,
        int maximumDistance)
    {
        bool foundLight = false;
        int x = centerX;
        int y = centerY;
        for (int distance = 1; distance <= maximumDistance; distance++)
        {
            x += stepX;
            y += stepY;
            if (!IsInside(raster, x, y))
            {
                break;
            }

            bool dark = raster.Pixels[y * raster.Stride + x] == 0;
            if (!foundLight)
            {
                if (!dark)
                {
                    foundLight = true;
                }
            }
            else if (dark)
            {
                return new int[] { x, y };
            }
        }

        return new int[] { -1, -1 };
    }

    static ConnectedRegion FloodFill(
        QrRaster raster,
        int seedX,
        int seedY,
        int minimumX,
        int minimumY,
        int maximumX,
        int maximumY)
    {
        ConnectedRegion region = new ConnectedRegion();
        int width = maximumX - minimumX + 1;
        int height = maximumY - minimumY + 1;
        long area = (long)width * height;
        if (area <= 0 || area > MaximumRegionPixels)
        {
            return region;
        }

        int cellCount = (int)area;
        bool[] visited = new bool[cellCount];
        int[] queue = new int[cellCount];
        int seedIndex = (seedY - minimumY) * width + seedX - minimumX;
        visited[seedIndex] = true;
        queue[0] = seedIndex;
        int head = 0;
        int tail = 1;
        while (head < tail)
        {
            int localIndex = queue[head];
            head++;
            int localY = localIndex / width;
            int localX = localIndex - localY * width;
            int x = minimumX + localX;
            int y = minimumY + localY;
            region.Area++;
            if (x < region.MinimumX)
            {
                region.MinimumX = x;
            }

            if (x > region.MaximumX)
            {
                region.MaximumX = x;
            }

            if (y < region.MinimumY)
            {
                region.MinimumY = y;
            }

            if (y > region.MaximumY)
            {
                region.MaximumY = y;
            }

            UpdateExtrema(region, x, y);
            if (x == minimumX || x == maximumX || y == minimumY || y == maximumY)
            {
                region.TouchesBoundary = true;
            }

            tail = EnqueueDark(
                raster,
                x - 1,
                y,
                minimumX,
                minimumY,
                maximumX,
                maximumY,
                width,
                visited,
                queue,
                tail);
            tail = EnqueueDark(
                raster,
                x + 1,
                y,
                minimumX,
                minimumY,
                maximumX,
                maximumY,
                width,
                visited,
                queue,
                tail);
            tail = EnqueueDark(
                raster,
                x,
                y - 1,
                minimumX,
                minimumY,
                maximumX,
                maximumY,
                width,
                visited,
                queue,
                tail);
            tail = EnqueueDark(
                raster,
                x,
                y + 1,
                minimumX,
                minimumY,
                maximumX,
                maximumY,
                width,
                visited,
                queue,
                tail);
        }

        region.Complete = region.Area > 0;
        return region;
    }

    static int EnqueueDark(
        QrRaster raster,
        int x,
        int y,
        int minimumX,
        int minimumY,
        int maximumX,
        int maximumY,
        int width,
        bool[] visited,
        int[] queue,
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
        if (raster.Pixels[y * raster.Stride + x] == 0)
        {
            queue[tail] = localIndex;
            tail++;
        }

        return tail;
    }

    static void UpdateExtrema(ConnectedRegion region, int x, int y)
    {
        int sum = x + y;
        if (sum < region.MinimumSum)
        {
            region.MinimumSum = sum;
            region.MinimumSumX = x;
            region.MinimumSumY = y;
        }

        if (sum > region.MaximumSum)
        {
            region.MaximumSum = sum;
            region.MaximumSumX = x;
            region.MaximumSumY = y;
        }

        int difference = x - y;
        if (difference < region.MinimumDifference)
        {
            region.MinimumDifference = difference;
            region.MinimumDifferenceX = x;
            region.MinimumDifferenceY = y;
        }

        if (difference > region.MaximumDifference)
        {
            region.MaximumDifference = difference;
            region.MaximumDifferenceX = x;
            region.MaximumDifferenceY = y;
        }
    }

    static double CalculateRegionConfidence(
        ConnectedRegion center,
        ConnectedRegion outer,
        int candidateCenterX,
        int candidateCenterY,
        double moduleSize)
    {
        int centerWidth = center.MaximumX - center.MinimumX + 1;
        int centerHeight = center.MaximumY - center.MinimumY + 1;
        int outerWidth = outer.MaximumX - outer.MinimumX + 1;
        int outerHeight = outer.MaximumY - outer.MinimumY + 1;
        if (centerWidth < moduleSize * 1.5
            || centerWidth > moduleSize * 4.5
            || centerHeight < moduleSize * 1.5
            || centerHeight > moduleSize * 4.5
            || outerWidth < moduleSize * 4.5
            || outerWidth > moduleSize * 9.5
            || outerHeight < moduleSize * 4.5
            || outerHeight > moduleSize * 9.5)
        {
            return 0.0;
        }

        if (center.MinimumX <= outer.MinimumX
            || center.MaximumX >= outer.MaximumX
            || center.MinimumY <= outer.MinimumY
            || center.MaximumY >= outer.MaximumY)
        {
            return 0.0;
        }

        double centerAspect = (double)centerWidth / centerHeight;
        if (centerAspect > 1.0)
        {
            centerAspect = 1.0 / centerAspect;
        }

        double outerAspect = (double)outerWidth / outerHeight;
        if (outerAspect > 1.0)
        {
            outerAspect = 1.0 / outerAspect;
        }

        double centerDensity = (double)center.Area / (centerWidth * centerHeight);
        double outerDensity = (double)outer.Area / (outerWidth * outerHeight);
        if (centerAspect < 0.5
            || outerAspect < 0.5
            || centerDensity < 0.6
            || outerDensity < 0.2
            || outerDensity > 0.8)
        {
            return 0.0;
        }

        double outerCenterX = (outer.MinimumX + outer.MaximumX + 1) / 2.0;
        double outerCenterY = (outer.MinimumY + outer.MaximumY + 1) / 2.0;
        double horizontalOffset = outerCenterX - candidateCenterX;
        double verticalOffset = outerCenterY - candidateCenterY;
        double centerDistance = global::System.Math.Sqrt(
            horizontalOffset * horizontalOffset + verticalOffset * verticalOffset);
        if (centerDistance > moduleSize * 1.5)
        {
            return 0.0;
        }

        double centerAreaScore = Similarity(center.Area, moduleSize * moduleSize * 9.0);
        double outerAreaScore = Similarity(outer.Area, moduleSize * moduleSize * 24.0);
        double centerWidthScore = Similarity(centerWidth, moduleSize * 3.0);
        double centerHeightScore = Similarity(centerHeight, moduleSize * 3.0);
        double outerWidthScore = Similarity(outerWidth, moduleSize * 7.0);
        double outerHeightScore = Similarity(outerHeight, moduleSize * 7.0);
        double alignmentScore = 1.0 - centerDistance / (moduleSize * 1.5);
        return (centerAreaScore
            + outerAreaScore
            + centerWidthScore
            + centerHeightScore
            + outerWidthScore
            + outerHeightScore
            + alignmentScore) / 7.0;
    }

    static double Similarity(double actual, double expected)
    {
        if (actual < expected)
        {
            return actual / expected;
        }

        return expected / actual;
    }

    static bool IsInside(QrRaster raster, int x, int y)
    {
        return x >= 0 && x < raster.Width && y >= 0 && y < raster.Height;
    }

    static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
}

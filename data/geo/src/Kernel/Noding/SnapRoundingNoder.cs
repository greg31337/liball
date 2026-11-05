using System;
using System.Collections.Generic;

namespace geo;

internal class SnapRoundingResult
{
    internal SegmentString[] Output = [];
    internal HotPixel[] HotPixels = [];
    internal int RemovedCollapsedEdgeCount;
    internal bool IsValid;
}

internal static class SnapRoundingNoder
{
    internal static SnapRoundingResult NodeToGrid(
        SegmentString[] segmentStrings, PrecisionModel precisionModel)
    {
        double gridSize = ValidateFixedPrecision(precisionModel);
        SnapRoundingResult result = new SnapRoundingResult();
        SegmentString[] rounded = RoundInputs(
            segmentStrings, precisionModel, result);
        List<HotPixel> pixels = CollectHotPixels(
            rounded, precisionModel, gridSize);
        InsertHotPixelNodes(rounded, pixels);
        SegmentString[] split = IndexedNoder.SplitGroup(
            rounded, 0, rounded.Length);
        SegmentString[] cleaned = CollapsedEdgeCleaner.CleanEdges(split);
        result.RemovedCollapsedEdgeCount += split.Length - cleaned.Length;
        FullyNodedValidator.EnsureFullyNoded(cleaned);
        result.Output = cleaned;
        result.HotPixels = pixels.ToArray();
        result.IsValid = true;
        return result;
    }

    internal static double ValidateFixedPrecision(PrecisionModel precisionModel)
    {
        if (precisionModel == null)
        {
            throw new PrecisionException("Snap-rounding precision model is null.");
        }
        if (precisionModel.Type != PrecisionModelType.Fixed)
        {
            throw new PrecisionException("Snap rounding requires a fixed precision model.");
        }
        if (!double.IsFinite(precisionModel.Scale) || precisionModel.Scale <= 0.0 ||
            !double.IsFinite(precisionModel.OffsetX) ||
            !double.IsFinite(precisionModel.OffsetY))
        {
            throw new PrecisionException(
                "Fixed precision scale and offsets must be finite, with positive scale.");
        }
        double gridSize = 1.0 / precisionModel.Scale;
        if (!double.IsFinite(gridSize) || gridSize <= 0.0)
        {
            throw new PrecisionException("Fixed precision grid size is outside the finite range.");
        }
        return gridSize;
    }

    internal static SegmentString[] RoundInputs(
        SegmentString[] segmentStrings,
        PrecisionModel precisionModel, SnapRoundingResult result)
    {
        if (segmentStrings == null)
        {
            throw new InvalidGeometryException("Snap-rounding input array is null.");
        }
        List<SegmentString> rounded = new List<SegmentString>();
        int sourceIndex = 0;
        while (sourceIndex < segmentStrings.Length)
        {
            SegmentString source = segmentStrings[sourceIndex];
            if (source == null)
            {
                throw new InvalidGeometryException("Snap-rounding input is null.");
            }
            List<Coordinate> coordinates = new List<Coordinate>();
            int firstRetainedSegment = -1;
            int lastRetainedSegment = -1;
            int coordinateIndex = 0;
            while (coordinateIndex < source.Coordinates.Length)
            {
                Coordinate coordinate = RoundCoordinate(
                    source.Coordinates[coordinateIndex], precisionModel);
                if (coordinates.Count == 0 ||
                    !RobustLineIntersection.CoordinatesEqualPlanar(
                        coordinates[coordinates.Count - 1], coordinate))
                {
                    if (coordinates.Count > 0)
                    {
                        int retainedSegment = coordinateIndex - 1;
                        if (firstRetainedSegment < 0)
                        {
                            firstRetainedSegment = retainedSegment;
                        }
                        lastRetainedSegment = retainedSegment;
                    }
                    coordinates.Add(coordinate);
                }
                coordinateIndex++;
            }
            if (coordinates.Count >= 2)
            {
                SegmentString roundedString = new SegmentString(
                    coordinates.ToArray(), source.SourceMetadata, source.SourceIndex);
                roundedString.SourceStartSegment = source.SourceStartSegment +
                    firstRetainedSegment;
                roundedString.SourceEndSegment = source.SourceStartSegment +
                    lastRetainedSegment;
                rounded.Add(roundedString);
            }
            else
            {
                result.RemovedCollapsedEdgeCount++;
            }
            sourceIndex++;
        }
        return rounded.ToArray();
    }

    internal static Coordinate RoundCoordinate(
        Coordinate coordinate, PrecisionModel precisionModel)
    {
        VectorMath.ValidateCoordinateValue(coordinate, "Snap-rounding coordinate");
        Coordinate rounded = RobustLineIntersection.CopyCoordinate(coordinate);
        rounded.X = RoundOrdinate(
            coordinate.X, precisionModel.OffsetX, precisionModel.Scale);
        rounded.Y = RoundOrdinate(
            coordinate.Y, precisionModel.OffsetY, precisionModel.Scale);
        return rounded;
    }

    internal static double RoundOrdinate(double value, double offset, double scale)
    {
        double scaled = (value - offset) * scale;
        double rounded = Math.Round(scaled, MidpointRounding.AwayFromZero) /
            scale + offset;
        if (!double.IsFinite(rounded))
        {
            throw new PrecisionException("Snap rounding produced a non-finite ordinate.");
        }
        return rounded;
    }

    internal static List<HotPixel> CollectHotPixels(
        SegmentString[] segmentStrings,
        PrecisionModel precisionModel, double gridSize)
    {
        List<HotPixel> pixels = new List<HotPixel>();
        SweepLineIndex index = new SweepLineIndex(segmentStrings);
        SegmentPairCandidate[] candidates = index.GetCandidatePairs(true);
        int candidateIndex = 0;
        while (candidateIndex < candidates.Length)
        {
            SegmentPairCandidate candidate = candidates[candidateIndex];
            if (!IndexedNoder.ShouldSkipSelfPair(candidate))
            {
                Coordinate firstStart = candidate.First.Coordinates[
                    candidate.FirstSegmentIndex];
                Coordinate firstEnd = candidate.First.Coordinates[
                    candidate.FirstSegmentIndex + 1];
                Coordinate secondStart = candidate.Second.Coordinates[
                    candidate.SecondSegmentIndex];
                Coordinate secondEnd = candidate.Second.Coordinates[
                    candidate.SecondSegmentIndex + 1];
                SegmentIntersectionResult intersection =
                    RobustLineIntersection.GetSegmentIntersection(
                        firstStart, firstEnd, secondStart, secondEnd);
                AddIntersectionHotPixels(
                    pixels, intersection, precisionModel, gridSize);
            }
            candidateIndex++;
        }
        int sourceIndex = 0;
        while (sourceIndex < segmentStrings.Length)
        {
            Coordinate[] coordinates = segmentStrings[sourceIndex].Coordinates;
            int coordinateIndex = 0;
            while (coordinateIndex < coordinates.Length)
            {
                AddHotPixel(pixels, coordinates[coordinateIndex], gridSize);
                coordinateIndex++;
            }
            sourceIndex++;
        }
        return pixels;
    }

    internal static void AddIntersectionHotPixels(
        List<HotPixel> pixels, SegmentIntersectionResult intersection,
        PrecisionModel precisionModel, double gridSize)
    {
        if (intersection.FirstIntersection != null)
        {
            Coordinate rounded = RoundCoordinate(
                intersection.FirstIntersection, precisionModel);
            AddHotPixel(pixels, rounded, gridSize);
        }
        if (intersection.SecondIntersection != null)
        {
            Coordinate rounded = RoundCoordinate(
                intersection.SecondIntersection, precisionModel);
            AddHotPixel(pixels, rounded, gridSize);
        }
    }

    internal static void AddHotPixel(
        List<HotPixel> pixels, Coordinate center, double gridSize)
    {
        int index = 0;
        while (index < pixels.Count)
        {
            if (RobustLineIntersection.CoordinatesEqualPlanar(
                pixels[index].Center, center)) return;
            index++;
        }
        pixels.Add(new HotPixel(center, gridSize));
    }

    internal static void InsertHotPixelNodes(
        SegmentString[] segmentStrings, List<HotPixel> pixels)
    {
        int sourceIndex = 0;
        while (sourceIndex < segmentStrings.Length)
        {
            SegmentString source = segmentStrings[sourceIndex];
            int segmentIndex = 0;
            while (segmentIndex + 1 < source.Coordinates.Length)
            {
                Coordinate start = source.Coordinates[segmentIndex];
                Coordinate end = source.Coordinates[segmentIndex + 1];
                int pixelIndex = 0;
                while (pixelIndex < pixels.Count)
                {
                    HotPixel pixel = pixels[pixelIndex];
                    if (pixel.IntersectsSegment(start, end))
                    {
                        SegmentNoding.AddSnappedNode(
                            source, pixel.Center, segmentIndex);
                    }
                    pixelIndex++;
                }
                segmentIndex++;
            }
            sourceIndex++;
        }
    }
}

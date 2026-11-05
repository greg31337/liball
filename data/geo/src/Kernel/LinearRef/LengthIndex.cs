namespace geo;

internal static class LengthIndex
{
    internal static double GetTotalLength(Geometry geometry)
    {
        Coordinate[][] components =
            LinearLocationMath.GetLineComponents(geometry);
        CompensatedMeasureSum sum = new CompensatedMeasureSum();
        int componentIndex = 0;
        while (componentIndex < components.Length)
        {
            AddComponentLength(components[componentIndex], sum);
            componentIndex++;
        }
        return sum.GetValue();
    }

    internal static LinearLocation GetLocationAtAbsoluteIndex(
        Geometry geometry, double lengthIndex,
        bool clamp, bool resolveLower)
    {
        if (!double.IsFinite(lengthIndex))
        {
            throw new InvalidGeometryException(
                "Absolute length index must be finite.");
        }
        Coordinate[][] components =
            LinearLocationMath.GetLineComponents(geometry);
        if (!LinearLocationMath.HasUsableComponent(components))
        {
            return LinearLocationMath.CreateInvalidLocation();
        }
        double totalLength = GetComponentsLength(components);
        double target = ResolveIndex(
            lengthIndex, totalLength, clamp,
            "Absolute length index is outside the geometry.");
        if (target == 0.0)
        {
            return LinearLocationMath.CreateStartLocation(geometry);
        }
        if (target == totalLength)
        {
            return LinearLocationMath.CreateEndLocation(geometry);
        }

        double traversed = 0.0;
        int componentIndex = 0;
        while (componentIndex < components.Length)
        {
            Coordinate[] coordinates = components[componentIndex];
            if (coordinates.Length > 0 && target == traversed &&
                !resolveLower)
            {
                return LinearLocationMath.CreateLocation(
                    componentIndex, 0, 0.0);
            }
            int segmentIndex = 0;
            while (segmentIndex + 1 < coordinates.Length)
            {
                double segmentLength =
                    SegmentDistanceAlgorithms.GetPlanarDistance(
                        coordinates[segmentIndex],
                        coordinates[segmentIndex + 1]);
                double segmentEnd = traversed + segmentLength;
                if (target < segmentEnd)
                {
                    double fraction = segmentLength == 0.0 ?
                        0.0 : (target - traversed) / segmentLength;
                    return LinearLocationMath.CreateLocation(
                        componentIndex, segmentIndex, fraction);
                }
                if (target == segmentEnd &&
                    segmentIndex + 1 < coordinates.Length - 1)
                {
                    int nextSegment = FindNextNonZeroSegment(
                        coordinates, segmentIndex + 1);
                    if (nextSegment >= 0)
                    {
                        return LinearLocationMath.CreateLocation(
                            componentIndex, nextSegment, 0.0);
                    }
                }
                traversed = segmentEnd;
                segmentIndex++;
            }
            if (coordinates.Length > 0 && target == traversed &&
                resolveLower)
            {
                return LinearLocationMath.CreateLocation(
                    componentIndex, coordinates.Length - 1, 0.0);
            }
            componentIndex++;
        }
        return LinearLocationMath.CreateEndLocation(geometry);
    }

    internal static double GetAbsoluteIndex(
        Geometry geometry, LinearLocation location)
    {
        Coordinate[][] components =
            LinearLocationMath.GetLineComponents(geometry);
        LinearLocation normalized =
            LinearLocationMath.NormalizeLocation(geometry, location);
        if (!normalized.IsValid) return 0.0;
        CompensatedMeasureSum sum = new CompensatedMeasureSum();
        int componentIndex = 0;
        while (componentIndex < normalized.ComponentIndex)
        {
            AddComponentLength(components[componentIndex], sum);
            componentIndex++;
        }
        Coordinate[] coordinates =
            components[normalized.ComponentIndex];
        int segmentIndex = 0;
        while (segmentIndex < normalized.SegmentIndex &&
            segmentIndex + 1 < coordinates.Length)
        {
            sum.AddValue(SegmentDistanceAlgorithms.GetPlanarDistance(
                coordinates[segmentIndex],
                coordinates[segmentIndex + 1]));
            segmentIndex++;
        }
        if (normalized.SegmentFraction > 0.0 &&
            normalized.SegmentIndex + 1 < coordinates.Length)
        {
            double segmentLength =
                SegmentDistanceAlgorithms.GetPlanarDistance(
                    coordinates[normalized.SegmentIndex],
                    coordinates[normalized.SegmentIndex + 1]);
            sum.AddValue(
                segmentLength * normalized.SegmentFraction);
        }
        return sum.GetValue();
    }

    internal static LinearLocation GetLocationAtNormalizedIndex(
        Geometry geometry, double normalizedIndex,
        bool clamp, bool resolveLower)
    {
        if (!double.IsFinite(normalizedIndex))
        {
            throw new InvalidGeometryException(
                "Normalized length index must be finite.");
        }
        double normalized = ResolveIndex(
            normalizedIndex, 1.0, clamp,
            "Normalized length index must be between zero and one.");
        double totalLength = GetTotalLength(geometry);
        if (double.IsPositiveInfinity(totalLength))
        {
            throw new InvalidGeometryException(
                "Normalized indexing requires finite total length.");
        }
        if (totalLength == 0.0)
        {
            if (normalized >= 1.0)
            {
                return LinearLocationMath.CreateEndLocation(geometry);
            }
            return LinearLocationMath.CreateStartLocation(geometry);
        }
        return GetLocationAtAbsoluteIndex(
            geometry, totalLength * normalized,
            true, resolveLower);
    }

    internal static double GetNormalizedIndex(
        Geometry geometry, LinearLocation location)
    {
        double totalLength = GetTotalLength(geometry);
        if (totalLength == 0.0) return 0.0;
        if (double.IsPositiveInfinity(totalLength))
        {
            throw new InvalidGeometryException(
                "Normalized indexing requires finite total length.");
        }
        return GetAbsoluteIndex(geometry, location) / totalLength;
    }

    internal static void AddComponentLength(
        Coordinate[] coordinates, CompensatedMeasureSum sum)
    {
        int segmentIndex = 0;
        while (segmentIndex + 1 < coordinates.Length)
        {
            sum.AddValue(SegmentDistanceAlgorithms.GetPlanarDistance(
                coordinates[segmentIndex],
                coordinates[segmentIndex + 1]));
            segmentIndex++;
        }
    }

    internal static int FindNextNonZeroSegment(
        Coordinate[] coordinates, int startIndex)
    {
        int index = startIndex;
        while (index + 1 < coordinates.Length)
        {
            double length = SegmentDistanceAlgorithms.GetPlanarDistance(
                coordinates[index], coordinates[index + 1]);
            if (length > 0.0) return index;
            index++;
        }
        return -1;
    }

    internal static double GetComponentsLength(
        Coordinate[][] components)
    {
        CompensatedMeasureSum sum = new CompensatedMeasureSum();
        int index = 0;
        while (index < components.Length)
        {
            AddComponentLength(components[index], sum);
            index++;
        }
        return sum.GetValue();
    }

    internal static double ResolveIndex(
        double index, double maximum, bool clamp, string message)
    {
        if (index < 0.0)
        {
            if (!clamp) throw new InvalidGeometryException(message);
            return 0.0;
        }
        if (index > maximum)
        {
            if (!clamp) throw new InvalidGeometryException(message);
            return maximum;
        }
        return index;
    }
}

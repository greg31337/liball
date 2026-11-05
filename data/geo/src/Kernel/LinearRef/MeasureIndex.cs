namespace geo;

internal static class MeasureIndex
{
    internal static bool IsMonotonicMeasure(Geometry geometry)
    {
        Coordinate[][] components =
            LinearLocationMath.GetLineComponents(geometry);
        if (!HasMeasuredLayout(geometry.CoordinateLayout)) return false;
        return ScanMonotonicDirection(components) != 2;
    }

    internal static int GetMonotonicDirection(Geometry geometry)
    {
        Coordinate[][] components =
            LinearLocationMath.GetLineComponents(geometry);
        if (!HasMeasuredLayout(geometry.CoordinateLayout))
        {
            throw new InvalidGeometryException(
                "Measure indexing requires an XYM or XYZM geometry.");
        }
        int direction = ScanMonotonicDirection(components);
        if (direction == 2)
        {
            throw new InvalidGeometryException(
                "Measure indexing requires globally monotonic M ordinates.");
        }
        return direction;
    }

    internal static LinearLocation GetLocationAtMeasure(
        Geometry geometry, double measure,
        bool clamp, bool resolveEarlier)
    {
        if (!double.IsFinite(measure))
        {
            throw new InvalidGeometryException(
                "Measure index must be finite.");
        }
        int direction = GetMonotonicDirection(geometry);
        Coordinate[][] components =
            LinearLocationMath.GetLineComponents(geometry);
        int firstComponent =
            LinearLocationMath.FindFirstUsableComponent(components);
        if (firstComponent < 0)
        {
            return LinearLocationMath.CreateInvalidLocation();
        }
        int lastComponent =
            LinearLocationMath.FindLastUsableComponent(components);
        double firstMeasure = components[firstComponent][0].M;
        Coordinate[] lastCoordinates = components[lastComponent];
        double lastMeasure =
            lastCoordinates[lastCoordinates.Length - 1].M;
        double minimum = direction < 0 ? lastMeasure : firstMeasure;
        double maximum = direction < 0 ? firstMeasure : lastMeasure;
        double target = ResolveMeasure(
            measure, minimum, maximum, clamp);

        LinearLocation? exact = GetExactMeasureLocation(
            components, target, resolveEarlier);
        if (exact != null) return exact;

        int componentIndex = firstComponent;
        int previousComponent = -1;
        while (componentIndex <= lastComponent)
        {
            Coordinate[] coordinates = components[componentIndex];
            if (coordinates.Length > 0)
            {
                if (previousComponent >= 0)
                {
                    Coordinate[] previous = components[previousComponent];
                    double previousMeasure =
                        previous[previous.Length - 1].M;
                    double nextMeasure = coordinates[0].M;
                    if (IsStrictlyBetween(
                        target, previousMeasure, nextMeasure))
                    {
                        if (resolveEarlier)
                        {
                            return LinearLocationMath.CreateLocation(
                                previousComponent,
                                previous.Length - 1, 0.0);
                        }
                        return LinearLocationMath.CreateLocation(
                            componentIndex, 0, 0.0);
                    }
                }

                int segmentIndex = 0;
                while (segmentIndex + 1 < coordinates.Length)
                {
                    double startMeasure = coordinates[segmentIndex].M;
                    double endMeasure = coordinates[segmentIndex + 1].M;
                    if (IsStrictlyBetween(
                        target, startMeasure, endMeasure))
                    {
                        double fraction = (target - startMeasure) /
                            (endMeasure - startMeasure);
                        return LinearLocationMath.CreateLocation(
                            componentIndex, segmentIndex, fraction);
                    }
                    segmentIndex++;
                }
                previousComponent = componentIndex;
            }
            componentIndex++;
        }

        if (target == firstMeasure)
        {
            return LinearLocationMath.CreateStartLocation(geometry);
        }
        return LinearLocationMath.CreateEndLocation(geometry);
    }

    internal static double GetMeasureAtLocation(
        Geometry geometry, LinearLocation location)
    {
        GetMonotonicDirection(geometry);
        Coordinate? coordinate =
            LinearLocationMath.GetCoordinateAtLocation(geometry, location);
        if (coordinate == null) return GeometryConstants.NoDataOrdinate;
        return coordinate.M;
    }

    internal static Coordinate? GetCoordinateAtMeasure(
        Geometry geometry, double measure,
        bool clamp, bool resolveEarlier)
    {
        LinearLocation location = GetLocationAtMeasure(
            geometry, measure, clamp, resolveEarlier);
        return LinearLocationMath.GetCoordinateAtLocation(
            geometry, location);
    }

    internal static Geometry ExtractMeasureInterval(
        Geometry geometry, double startMeasure, double endMeasure,
        bool clamp, bool resolveEarlier)
    {
        LinearLocation start = GetLocationAtMeasure(
            geometry, startMeasure, clamp, resolveEarlier);
        LinearLocation end = GetLocationAtMeasure(
            geometry, endMeasure, clamp, resolveEarlier);
        return LineSubstringBuilder.ExtractLine(
            geometry, start, end);
    }

    internal static int ScanMonotonicDirection(
        Coordinate[][] components)
    {
        bool hasPrevious = false;
        double previous = 0.0;
        int direction = 0;
        int componentIndex = 0;
        while (componentIndex < components.Length)
        {
            Coordinate[] coordinates = components[componentIndex];
            int coordinateIndex = 0;
            while (coordinateIndex < coordinates.Length)
            {
                double current = coordinates[coordinateIndex].M;
                if (!double.IsFinite(current)) return 2;
                if (hasPrevious)
                {
                    int comparison = CompareMeasures(previous, current);
                    if (comparison != 0)
                    {
                        if (direction == 0)
                        {
                            direction = comparison;
                        }
                        else if (direction != comparison)
                        {
                            return 2;
                        }
                    }
                }
                previous = current;
                hasPrevious = true;
                coordinateIndex++;
            }
            componentIndex++;
        }
        return direction;
    }

    internal static int CompareMeasures(double first, double second)
    {
        if (second > first) return 1;
        if (second < first) return -1;
        return 0;
    }

    internal static LinearLocation? GetExactMeasureLocation(
        Coordinate[][] components, double measure,
        bool resolveEarlier)
    {
        LinearLocation? found = null;
        int componentIndex = 0;
        while (componentIndex < components.Length)
        {
            Coordinate[] coordinates = components[componentIndex];
            int coordinateIndex = 0;
            while (coordinateIndex < coordinates.Length)
            {
                if (coordinates[coordinateIndex].M == measure)
                {
                    LinearLocation location =
                        LinearLocationMath.CreateLocation(
                            componentIndex, coordinateIndex, 0.0);
                    if (resolveEarlier) return location;
                    found = location;
                }
                coordinateIndex++;
            }
            componentIndex++;
        }
        return found;
    }

    internal static bool IsStrictlyBetween(
        double value, double first, double second)
    {
        if (first < second)
        {
            return value > first && value < second;
        }
        return value > second && value < first;
    }

    internal static double ResolveMeasure(
        double measure, double minimum, double maximum, bool clamp)
    {
        if (measure < minimum)
        {
            if (!clamp)
            {
                throw new InvalidGeometryException(
                    "Measure index is outside the geometry.");
            }
            return minimum;
        }
        if (measure > maximum)
        {
            if (!clamp)
            {
                throw new InvalidGeometryException(
                    "Measure index is outside the geometry.");
            }
            return maximum;
        }
        return measure;
    }

    internal static bool HasMeasuredLayout(CoordinateLayout layout)
    {
        return layout == CoordinateLayout.XYM ||
            layout == CoordinateLayout.XYZM;
    }
}

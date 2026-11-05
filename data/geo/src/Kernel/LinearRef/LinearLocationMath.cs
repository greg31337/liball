namespace geo;

internal static class LinearLocationMath
{
    internal static int CompareLocations(
        LinearLocation first, LinearLocation second)
    {
        ValidateComparableLocation(first);
        ValidateComparableLocation(second);
        if (!first.IsValid && !second.IsValid) return 0;
        if (!first.IsValid) return -1;
        if (!second.IsValid) return 1;
        if (first.ComponentIndex < second.ComponentIndex) return -1;
        if (first.ComponentIndex > second.ComponentIndex) return 1;
        if (first.SegmentIndex < second.SegmentIndex) return -1;
        if (first.SegmentIndex > second.SegmentIndex) return 1;
        if (first.SegmentFraction < second.SegmentFraction) return -1;
        if (first.SegmentFraction > second.SegmentFraction) return 1;
        return 0;
    }

    internal static LinearLocation NormalizeLocation(
        Geometry geometry, LinearLocation location)
    {
        Coordinate[][] components = GetLineComponents(geometry);
        if (location == null)
        {
            throw new InvalidGeometryException(
                "Linear location is null.");
        }
        if (!double.IsFinite(location.SegmentFraction))
        {
            throw new InvalidGeometryException(
                "Linear location segment fraction must be finite.");
        }
        if (!location.IsValid || !HasUsableComponent(components))
        {
            return CreateInvalidLocation();
        }

        int firstComponent = FindFirstUsableComponent(components);
        int lastComponent = FindLastUsableComponent(components);
        int componentIndex = location.ComponentIndex;
        if (componentIndex < firstComponent) componentIndex = firstComponent;
        if (componentIndex > lastComponent) componentIndex = lastComponent;
        if (components[componentIndex].Length == 0)
        {
            int next = FindNextUsableComponent(
                components, componentIndex);
            if (next >= 0)
            {
                componentIndex = next;
            }
            else
            {
                componentIndex = FindPreviousUsableComponent(
                    components, componentIndex);
            }
        }

        Coordinate[] coordinates = components[componentIndex];
        int segmentIndex = location.SegmentIndex;
        double segmentFraction = location.SegmentFraction;
        if (segmentIndex < 0)
        {
            segmentIndex = 0;
            segmentFraction = 0.0;
        }
        int endpointIndex = coordinates.Length - 1;
        if (segmentIndex >= endpointIndex)
        {
            return CreateLocation(componentIndex, endpointIndex, 0.0);
        }
        if (segmentFraction <= 0.0)
        {
            segmentFraction = 0.0;
        }
        else if (segmentFraction >= 1.0)
        {
            segmentIndex++;
            segmentFraction = 0.0;
        }
        if (segmentIndex >= endpointIndex)
        {
            segmentIndex = endpointIndex;
            segmentFraction = 0.0;
        }
        return CreateLocation(
            componentIndex, segmentIndex, segmentFraction);
    }

    internal static LinearLocation CreateStartLocation(Geometry geometry)
    {
        Coordinate[][] components = GetLineComponents(geometry);
        int componentIndex = FindFirstUsableComponent(components);
        if (componentIndex < 0) return CreateInvalidLocation();
        return CreateLocation(componentIndex, 0, 0.0);
    }

    internal static LinearLocation CreateEndLocation(Geometry geometry)
    {
        Coordinate[][] components = GetLineComponents(geometry);
        int componentIndex = FindLastUsableComponent(components);
        if (componentIndex < 0) return CreateInvalidLocation();
        return CreateLocation(
            componentIndex,
            components[componentIndex].Length - 1, 0.0);
    }

    internal static LinearLocation ResolveComponentEndpoint(
        Geometry geometry, LinearLocation location, bool resolveLower)
    {
        Coordinate[][] components = GetLineComponents(geometry);
        LinearLocation normalized = NormalizeLocation(geometry, location);
        if (!normalized.IsValid) return normalized;
        Coordinate[] coordinates =
            components[normalized.ComponentIndex];
        bool atStart = normalized.SegmentIndex == 0 &&
            normalized.SegmentFraction == 0.0;
        bool atEnd = normalized.SegmentIndex ==
            coordinates.Length - 1;
        if (resolveLower && atStart)
        {
            int previous = FindPreviousUsableComponent(
                components, normalized.ComponentIndex);
            if (previous >= 0)
            {
                return CreateLocation(
                    previous, components[previous].Length - 1, 0.0);
            }
        }
        if (!resolveLower && atEnd)
        {
            int next = FindNextUsableComponent(
                components, normalized.ComponentIndex);
            if (next >= 0)
            {
                return CreateLocation(next, 0, 0.0);
            }
        }
        return normalized;
    }

    internal static bool IsEndpoint(
        Geometry geometry, LinearLocation location)
    {
        Coordinate[][] components = GetLineComponents(geometry);
        LinearLocation normalized = NormalizeLocation(geometry, location);
        if (!normalized.IsValid) return false;
        return normalized.SegmentIndex ==
            components[normalized.ComponentIndex].Length - 1;
    }

    internal static LinearLocation ProjectCoordinate(
        Geometry geometry, Coordinate coordinate)
    {
        VectorMath.ValidateCoordinateValue(
            coordinate, "Linear projection coordinate");
        Coordinate[][] components = GetLineComponents(geometry);
        LinearLocation? bestLocation = null;
        double bestDistance = double.PositiveInfinity;
        int componentIndex = 0;
        while (componentIndex < components.Length)
        {
            Coordinate[] coordinates = components[componentIndex];
            int segmentIndex = 0;
            while (segmentIndex + 1 < coordinates.Length)
            {
                DistanceResult result =
                    SegmentDistanceAlgorithms.GetNearestPointSegment(
                        coordinate, coordinates[segmentIndex],
                        coordinates[segmentIndex + 1]);
                LinearLocation? segmentLocation = result.SecondLocation;
                if (segmentLocation == null)
                {
                    throw new InvalidGeometryException(
                        "Linear projection did not produce a segment location.");
                }
                LinearLocation candidate = CreateLocation(
                    componentIndex, segmentIndex,
                    segmentLocation.SegmentFraction);
                candidate = NormalizeLocation(geometry, candidate);
                if (bestLocation == null ||
                    result.Distance < bestDistance ||
                    (result.Distance == bestDistance &&
                        CompareLocations(candidate, bestLocation) < 0))
                {
                    bestLocation = candidate;
                    bestDistance = result.Distance;
                }
                segmentIndex++;
            }
            componentIndex++;
        }
        if (bestLocation == null) return CreateInvalidLocation();
        return bestLocation;
    }

    internal static Coordinate? GetCoordinateAtLocation(
        Geometry geometry, LinearLocation location)
    {
        Coordinate[][] components = GetLineComponents(geometry);
        LinearLocation normalized = NormalizeLocation(geometry, location);
        if (!normalized.IsValid) return null;
        Coordinate[] coordinates =
            components[normalized.ComponentIndex];
        Coordinate start = coordinates[normalized.SegmentIndex];
        if (normalized.SegmentFraction == 0.0 ||
            normalized.SegmentIndex + 1 >= coordinates.Length)
        {
            return CoordinateSequences.CopyCoordinateValue(start);
        }
        return VectorMath.InterpolateCoordinate(
            start, coordinates[normalized.SegmentIndex + 1],
            normalized.SegmentFraction);
    }

    internal static Coordinate? GetOffsetCoordinate(
        Geometry geometry, LinearLocation location, double offset)
    {
        if (!double.IsFinite(offset))
        {
            throw new InvalidGeometryException(
                "Linear offset must be finite.");
        }
        Coordinate[][] components = GetLineComponents(geometry);
        LinearLocation normalized = NormalizeLocation(geometry, location);
        if (!normalized.IsValid) return null;
        Coordinate? baseCoordinate =
            GetCoordinateAtLocation(geometry, normalized);
        if (baseCoordinate == null) return null;
        if (offset == 0.0) return baseCoordinate;
        Coordinate[] coordinates =
            components[normalized.ComponentIndex];
        int directionSegment = FindOffsetSegment(
            coordinates, normalized);
        if (directionSegment < 0)
        {
            throw new InvalidGeometryException(
                "A nonzero offset requires a non-degenerate line segment.");
        }
        Coordinate segmentStart = coordinates[directionSegment];
        Coordinate segmentEnd = coordinates[directionSegment + 1];
        double scale = GetDirectionScale(segmentStart, segmentEnd);
        double deltaX = segmentEnd.X / scale -
            segmentStart.X / scale;
        double deltaY = segmentEnd.Y / scale -
            segmentStart.Y / scale;
        double directionLength = System.Math.Sqrt(
            deltaX * deltaX + deltaY * deltaY);
        double normalX = -deltaY / directionLength;
        double normalY = deltaX / directionLength;
        baseCoordinate.X = VectorMath.RequireFiniteResult(
            baseCoordinate.X + normalX * offset,
            "Offset X ordinate");
        baseCoordinate.Y = VectorMath.RequireFiniteResult(
            baseCoordinate.Y + normalY * offset,
            "Offset Y ordinate");
        return baseCoordinate;
    }

    internal static int FindOffsetSegment(
        Coordinate[] coordinates, LinearLocation location)
    {
        if (coordinates.Length < 2) return -1;
        int startIndex = location.SegmentIndex;
        if (startIndex >= coordinates.Length - 1)
        {
            startIndex = coordinates.Length - 2;
        }
        int index = startIndex;
        while (index + 1 < coordinates.Length)
        {
            if (!RobustLineIntersection.CoordinatesEqualPlanar(
                coordinates[index], coordinates[index + 1]))
            {
                return index;
            }
            index++;
        }
        index = startIndex - 1;
        while (index >= 0)
        {
            if (!RobustLineIntersection.CoordinatesEqualPlanar(
                coordinates[index], coordinates[index + 1]))
            {
                return index;
            }
            index--;
        }
        return -1;
    }

    internal static double GetDirectionScale(
        Coordinate start, Coordinate end)
    {
        double scale = System.Math.Abs(start.X);
        double candidate = System.Math.Abs(start.Y);
        if (candidate > scale) scale = candidate;
        candidate = System.Math.Abs(end.X);
        if (candidate > scale) scale = candidate;
        candidate = System.Math.Abs(end.Y);
        if (candidate > scale) scale = candidate;
        if (scale == 0.0) return 1.0;
        return scale;
    }

    internal static Coordinate[][] GetLineComponents(Geometry geometry)
    {
        ValidateLinearGeometry(geometry);
        if (geometry.GeometryType == GeometryType.LineString)
        {
            LineString line = (LineString)
                GeometryDispatch.GetSelectedPayload(geometry);
            return [line.Coordinates];
        }
        MultiLineString multiLine = (MultiLineString)
            GeometryDispatch.GetSelectedPayload(geometry);
        Coordinate[][] components =
            new Coordinate[multiLine.LineStrings.Length][];
        int index = 0;
        while (index < multiLine.LineStrings.Length)
        {
            components[index] = multiLine.LineStrings[index].Coordinates;
            index++;
        }
        return components;
    }

    internal static Coordinate[] GetComponentCoordinates(
        Geometry geometry, int componentIndex)
    {
        Coordinate[][] components = GetLineComponents(geometry);
        if (componentIndex < 0 || componentIndex >= components.Length)
        {
            throw new InvalidGeometryException(
                "Linear component index is outside the geometry.");
        }
        return components[componentIndex];
    }

    internal static int FindFirstUsableComponent(
        Coordinate[][] components)
    {
        int index = 0;
        while (index < components.Length)
        {
            if (components[index].Length > 0) return index;
            index++;
        }
        return -1;
    }

    internal static int FindLastUsableComponent(
        Coordinate[][] components)
    {
        int index = components.Length - 1;
        while (index >= 0)
        {
            if (components[index].Length > 0) return index;
            index--;
        }
        return -1;
    }

    internal static int FindNextUsableComponent(
        Coordinate[][] components, int componentIndex)
    {
        int index = componentIndex + 1;
        while (index < components.Length)
        {
            if (components[index].Length > 0) return index;
            index++;
        }
        return -1;
    }

    internal static int FindPreviousUsableComponent(
        Coordinate[][] components, int componentIndex)
    {
        int index = componentIndex - 1;
        while (index >= 0)
        {
            if (components[index].Length > 0) return index;
            index--;
        }
        return -1;
    }

    internal static bool HasUsableComponent(Coordinate[][] components)
    {
        return FindFirstUsableComponent(components) >= 0;
    }

    internal static LinearLocation CreateLocation(
        int componentIndex, int segmentIndex, double segmentFraction)
    {
        LinearLocation location = new LinearLocation();
        location.ComponentIndex = componentIndex;
        location.SegmentIndex = segmentIndex;
        location.SegmentFraction = segmentFraction;
        return location;
    }

    internal static LinearLocation CreateInvalidLocation()
    {
        LinearLocation location = new LinearLocation();
        location.IsValid = false;
        return location;
    }

    internal static void ValidateComparableLocation(
        LinearLocation location)
    {
        if (location == null)
        {
            throw new InvalidGeometryException(
                "Linear location is null.");
        }
        if (location.IsValid &&
            !double.IsFinite(location.SegmentFraction))
        {
            throw new InvalidGeometryException(
                "Linear location segment fraction must be finite.");
        }
    }

    internal static void ValidateLinearGeometry(Geometry geometry)
    {
        ValidationResult validation =
            GeometryValidityChecker.ValidateGeometry(geometry);
        if (!validation.IsValid)
        {
            throw new InvalidGeometryException(
                "Linear reference geometry is invalid: " +
                validation.Message);
        }
        if (geometry.GeometryType != GeometryType.LineString &&
            geometry.GeometryType != GeometryType.MultiLineString)
        {
            throw new InvalidGeometryException(
                "Linear referencing requires LineString or MultiLineString geometry.");
        }
    }
}

using System;
using System.Collections.Generic;

namespace geo;

internal static class GeographicLineAlgorithms
{
    internal static Geometry CreateGreatCircle(
        Coordinate start, Coordinate end, int segmentCount)
    {
        GeographicMath.ValidateGeographicCoordinate(start);
        GeographicMath.ValidateGeographicCoordinate(end);
        if (segmentCount < 1 ||
            segmentCount >= GeometryConstants.DefaultMaximumCoordinates)
        {
            throw new InvalidGeometryException(
                "Great-circle segment count must be positive and within the coordinate limit.");
        }

        Coordinate[] coordinates = new Coordinate[segmentCount + 1];
        int index = 0;
        while (index <= segmentCount)
        {
            coordinates[index] = InterpolateGreatCircle(
                start, end, (double)index / segmentCount);
            index++;
        }
        GeometryFactoryOptions options = new GeometryFactoryOptions();
        options.SpatialReferenceId =
            GeometryConstants.Crs84SpatialReferenceId;
        return GeometryBuilder.CreateLineStringValue(
            coordinates, options);
    }

    internal static Coordinate InterpolateGreatCircle(
        Coordinate start, Coordinate end, double fraction)
    {
        GeographicMath.ValidateGeographicCoordinate(start);
        GeographicMath.ValidateGeographicCoordinate(end);
        if (!double.IsFinite(fraction) ||
            fraction < 0.0 || fraction > 1.0)
        {
            throw new InvalidGeometryException(
                "Great-circle interpolation fraction must be from zero through one.");
        }
        if (fraction == 0.0)
        {
            return CreateNormalizedCopy(start);
        }
        if (fraction == 1.0)
        {
            return CreateNormalizedCopy(end);
        }

        double centralAngle = GeographicMath.GetCentralAngle(start, end);
        Coordinate result;
        if (centralAngle == 0.0)
        {
            result = CreateNormalizedCopy(start);
        }
        else
        {
            double bearing = GeographicMath.GetInitialBearing(
                start, end, SpatialUnit.Radians);
            result = GeographicMath.GetDestination(
                start, centralAngle * fraction, SpatialUnit.Radians,
                bearing, SpatialUnit.Radians);
        }
        result.Z = VectorMath.InterpolateOptionalOrdinate(
            start.Z, end.Z, fraction);
        result.M = VectorMath.InterpolateOptionalOrdinate(
            start.M, end.M, fraction);
        return result;
    }

    internal static double GetRhumbDistance(
        Coordinate first, Coordinate second, SpatialUnit outputUnit)
    {
        GeographicMath.ValidateGeographicCoordinate(first);
        GeographicMath.ValidateGeographicCoordinate(second);
        if (Math.Abs(first.Y) == 90.0 && first.Y == second.Y)
        {
            return GeographicMath.ConvertUnits(
                0.0, SpatialUnit.Radians, outputUnit);
        }
        double firstLatitude = GeographicMath.DegreesToRadians(first.Y);
        double secondLatitude = GeographicMath.DegreesToRadians(second.Y);
        double latitudeDelta = secondLatitude - firstLatitude;
        double longitudeDelta =
            GeographicMath.GetLongitudeDeltaRadians(first, second);
        double mercatorDelta = GetMercatorLatitude(secondLatitude) -
            GetMercatorLatitude(firstLatitude);
        double q = Math.Abs(mercatorDelta) > 1.0e-12 ?
            latitudeDelta / mercatorDelta : Math.Cos(firstLatitude);
        double angularDistance = Math.Sqrt(
            latitudeDelta * latitudeDelta +
            q * q * longitudeDelta * longitudeDelta);
        return GeographicMath.ConvertUnits(
            angularDistance, SpatialUnit.Radians, outputUnit);
    }

    internal static double GetRhumbBearing(
        Coordinate first, Coordinate second, SpatialUnit outputUnit)
    {
        GeographicMath.ValidateAngularUnit(outputUnit);
        GeographicMath.ValidateGeographicCoordinate(first);
        GeographicMath.ValidateGeographicCoordinate(second);
        if (GetRhumbDistance(
            first, second, SpatialUnit.Radians) == 0.0)
        {
            return 0.0;
        }
        double firstLatitude = GeographicMath.DegreesToRadians(first.Y);
        double secondLatitude = GeographicMath.DegreesToRadians(second.Y);
        double mercatorDelta = GetMercatorLatitude(secondLatitude) -
            GetMercatorLatitude(firstLatitude);
        double longitudeDelta =
            GeographicMath.GetLongitudeDeltaRadians(first, second);
        double bearing = GeographicMath.NormalizeRadians(
            Math.Atan2(longitudeDelta, mercatorDelta));
        return GeographicMath.ConvertAngularResult(
            bearing, outputUnit);
    }

    internal static Coordinate GetRhumbDestination(
        Coordinate origin, double distance, SpatialUnit distanceUnit,
        double bearing, SpatialUnit bearingUnit)
    {
        GeographicMath.ValidateGeographicCoordinate(origin);
        GeographicMath.ValidateAngularUnit(bearingUnit);
        if (!double.IsFinite(distance) || !double.IsFinite(bearing))
        {
            throw new InvalidGeometryException(
                "Rhumb destination distance and bearing must be finite.");
        }
        double angularDistance = GeographicMath.ConvertUnits(
            distance, distanceUnit, SpatialUnit.Radians);
        double bearingRadians = GeographicMath.ConvertUnits(
            bearing, bearingUnit, SpatialUnit.Radians);
        double firstLatitude =
            GeographicMath.DegreesToRadians(origin.Y);
        double latitudeDelta =
            angularDistance * Math.Cos(bearingRadians);
        double unreflectedLatitude = firstLatitude + latitudeDelta;
        double secondLatitude = NormalizeLatitudeRadians(
            unreflectedLatitude);
        double mercatorDelta = GetMercatorLatitude(secondLatitude) -
            GetMercatorLatitude(firstLatitude);
        double q = Math.Abs(mercatorDelta) > 1.0e-12 ?
            latitudeDelta / mercatorDelta : Math.Cos(firstLatitude);
        double longitudeDelta = 0.0;
        double eastwardDistance =
            angularDistance * Math.Sin(bearingRadians);
        if (Math.Abs(q) > 1.0e-15)
        {
            longitudeDelta = eastwardDistance / q;
        }
        else if (Math.Abs(eastwardDistance) > 1.0e-15)
        {
            throw new InvalidGeometryException(
                "An east-west rhumb destination is undefined at a pole.");
        }
        double longitude = GeographicMath.DegreesToRadians(
            GeographicMath.NormalizeLongitude(origin.X)) +
            longitudeDelta;

        Coordinate result = new Coordinate();
        result.X = GeographicMath.NormalizeLongitude(
            GeographicMath.RadiansToDegrees(longitude));
        result.Y = GeographicMath.RadiansToDegrees(secondLatitude);
        return result;
    }

    internal static double GetGeographicLength(
        Geometry lineal, SpatialUnit outputUnit)
    {
        Coordinate[][] components = GetValidatedLineComponents(lineal);
        double radians = GetComponentsLengthRadians(components);
        return GeographicMath.ConvertUnits(
            radians, SpatialUnit.Radians, outputUnit);
    }

    internal static Coordinate? GetCoordinateAlongLine(
        Geometry lineal, double distance, SpatialUnit distanceUnit)
    {
        if (!double.IsFinite(distance))
        {
            throw new InvalidGeometryException(
                "Geographic line distance must be finite.");
        }
        Coordinate[][] components = GetValidatedLineComponents(lineal);
        GeographicMath.GetRadiansPerUnit(distanceUnit);
        if (!LinearLocationMath.HasUsableComponent(components))
        {
            return null;
        }
        double target = GeographicMath.ConvertUnits(
            distance, distanceUnit, SpatialUnit.Radians);
        double total = GetComponentsLengthRadians(components);
        if (target < 0.0) target = 0.0;
        if (target > total) target = total;
        LinearLocation location = GetLocationAtDistance(
            components, target, total);
        return GetCoordinateAtLocation(components, location);
    }

    internal static Geometry SliceGeographicLine(
        Geometry lineal, double startDistance, double endDistance,
        SpatialUnit distanceUnit)
    {
        if (!double.IsFinite(startDistance) ||
            !double.IsFinite(endDistance))
        {
            throw new InvalidGeometryException(
                "Geographic slice distances must be finite.");
        }
        Coordinate[][] components = GetValidatedLineComponents(lineal);
        GeographicMath.GetRadiansPerUnit(distanceUnit);
        if (!LinearLocationMath.HasUsableComponent(components))
        {
            return LineSubstringBuilder.CreateLineGeometry(lineal, []);
        }
        double start = GeographicMath.ConvertUnits(
            startDistance, distanceUnit, SpatialUnit.Radians);
        double end = GeographicMath.ConvertUnits(
            endDistance, distanceUnit, SpatialUnit.Radians);
        double total = GetComponentsLengthRadians(components);
        start = ClampDistance(start, total);
        end = ClampDistance(end, total);
        LinearLocation startLocation = GetLocationAtDistance(
            components, start, total);
        LinearLocation endLocation = GetLocationAtDistance(
            components, end, total);
        bool reverse = LinearLocationMath.CompareLocations(
            startLocation, endLocation) > 0;
        LinearLocation lower = reverse ? endLocation : startLocation;
        LinearLocation upper = reverse ? startLocation : endLocation;
        List<LineString> lines = CreateSliceLines(
            components, lower, upper);
        if (reverse) LineSubstringBuilder.ReverseLines(lines);
        return LineSubstringBuilder.CreateResultGeometry(lineal, lines);
    }

    internal static DistanceResult GetNearestPointOnLine(
        Geometry lineal, Coordinate coordinate,
        SpatialUnit outputUnit)
    {
        GeographicMath.ValidateGeographicCoordinate(coordinate);
        Coordinate[][] components = GetValidatedLineComponents(lineal);
        GeographicMath.GetRadiansPerUnit(outputUnit);
        DistanceResult best = new DistanceResult();
        int componentIndex = 0;
        while (componentIndex < components.Length)
        {
            Coordinate[] component = components[componentIndex];
            int segmentIndex = 0;
            while (segmentIndex + 1 < component.Length)
            {
                DistanceResult candidate = GetNearestSegmentPoint(
                    component[segmentIndex],
                    component[segmentIndex + 1], coordinate);
                if (candidate.HasResult &&
                    (!best.HasResult ||
                        candidate.Distance < best.Distance))
                {
                    best = candidate;
                    LinearLocation location = new LinearLocation();
                    location.ComponentIndex = componentIndex;
                    location.SegmentIndex = segmentIndex;
                    if (candidate.FirstLocation != null)
                    {
                        location.SegmentFraction =
                            candidate.FirstLocation.SegmentFraction;
                    }
                    best.FirstLocation =
                        LinearLocationMath.NormalizeLocation(
                            lineal, location);
                }
                segmentIndex++;
            }
            componentIndex++;
        }
        if (!best.HasResult) return best;
        best.Distance = GeographicMath.ConvertUnits(
            best.Distance, SpatialUnit.Radians, outputUnit);
        best.SecondCoordinate =
            CoordinateSequences.CopyCoordinateValue(coordinate);
        return best;
    }

    internal static DistanceResult GetNearestSegmentPoint(
        Coordinate start, Coordinate end, Coordinate coordinate)
    {
        double[] startVector = ToUnitVector(start);
        double[] endVector = ToUnitVector(end);
        double[] pointVector = ToUnitVector(coordinate);
        double totalAngle = VectorAngle(startVector, endVector);
        DistanceResult result = CreateEndpointResult(
            start, end, coordinate, totalAngle);
        if (totalAngle <= 1.0e-15 ||
            Math.Abs(Math.PI - totalAngle) <= 1.0e-12)
        {
            return result;
        }

        double[] normal = CrossVector(startVector, endVector);
        double normalSquared = DotVector(normal, normal);
        if (normalSquared <= 1.0e-30) return result;
        double scale = DotVector(pointVector, normal) / normalSquared;
        double[] projected =
        [
            pointVector[0] - normal[0] * scale,
            pointVector[1] - normal[1] * scale,
            pointVector[2] - normal[2] * scale
        ];
        double[] firstCandidate = NormalizeVector(projected);
        if (firstCandidate.Length == 0) return result;
        ConsiderProjectedCandidate(
            startVector, endVector, pointVector,
            firstCandidate, totalAngle, result);
        double[] secondCandidate =
        [
            -firstCandidate[0],
            -firstCandidate[1],
            -firstCandidate[2]
        ];
        ConsiderProjectedCandidate(
            startVector, endVector, pointVector,
            secondCandidate, totalAngle, result);
        if (result.FirstCoordinate != null &&
            result.FirstLocation != null)
        {
            double fraction = result.FirstLocation.SegmentFraction;
            result.FirstCoordinate.Z =
                VectorMath.InterpolateOptionalOrdinate(
                    start.Z, end.Z, fraction);
            result.FirstCoordinate.M =
                VectorMath.InterpolateOptionalOrdinate(
                    start.M, end.M, fraction);
        }
        return result;
    }

    internal static void ConsiderProjectedCandidate(
        double[] start, double[] end, double[] point,
        double[] candidate, double totalAngle, DistanceResult result)
    {
        double startAngle = VectorAngle(start, candidate);
        double endAngle = VectorAngle(candidate, end);
        if (Math.Abs(startAngle + endAngle - totalAngle) > 1.0e-10)
        {
            return;
        }
        double distance = VectorAngle(point, candidate);
        double fraction = startAngle / totalAngle;
        double currentFraction = result.FirstLocation == null ?
            0.0 : result.FirstLocation.SegmentFraction;
        if (distance < result.Distance ||
            (distance == result.Distance && fraction < currentFraction))
        {
            result.Distance = distance;
            result.FirstCoordinate = FromUnitVector(candidate);
            LinearLocation location = new LinearLocation();
            location.SegmentFraction = fraction;
            result.FirstLocation = location;
        }
    }

    internal static DistanceResult CreateEndpointResult(
        Coordinate start, Coordinate end, Coordinate coordinate,
        double totalAngle)
    {
        double startDistance = GeographicMath.GetCentralAngle(
            coordinate, start);
        double endDistance = GeographicMath.GetCentralAngle(
            coordinate, end);
        DistanceResult result = new DistanceResult();
        result.HasResult = true;
        LinearLocation location = new LinearLocation();
        if (startDistance <= endDistance)
        {
            result.Distance = startDistance;
            result.FirstCoordinate = CreateNormalizedCopy(start);
            location.SegmentFraction = 0.0;
        }
        else
        {
            result.Distance = endDistance;
            result.FirstCoordinate = CreateNormalizedCopy(end);
            location.SegmentFraction = totalAngle == 0.0 ? 0.0 : 1.0;
        }
        result.FirstLocation = location;
        return result;
    }

    internal static Coordinate[][] GetValidatedLineComponents(
        Geometry lineal)
    {
        Coordinate[][] components =
            LinearLocationMath.GetLineComponents(lineal);
        int componentIndex = 0;
        while (componentIndex < components.Length)
        {
            Coordinate[] component = components[componentIndex];
            int coordinateIndex = 0;
            while (coordinateIndex < component.Length)
            {
                GeographicMath.ValidateGeographicCoordinate(
                    component[coordinateIndex]);
                coordinateIndex++;
            }
            componentIndex++;
        }
        return components;
    }

    internal static double GetComponentsLengthRadians(
        Coordinate[][] components)
    {
        CompensatedMeasureSum sum = new CompensatedMeasureSum();
        int componentIndex = 0;
        while (componentIndex < components.Length)
        {
            Coordinate[] component = components[componentIndex];
            int segmentIndex = 0;
            while (segmentIndex + 1 < component.Length)
            {
                sum.AddValue(GeographicMath.GetCentralAngle(
                    component[segmentIndex], component[segmentIndex + 1]));
                segmentIndex++;
            }
            componentIndex++;
        }
        return sum.GetValue();
    }

    internal static LinearLocation GetLocationAtDistance(
        Coordinate[][] components, double target, double total)
    {
        int first = LinearLocationMath.FindFirstUsableComponent(components);
        if (first < 0) return LinearLocationMath.CreateInvalidLocation();
        if (target <= 0.0)
        {
            return LinearLocationMath.CreateLocation(first, 0, 0.0);
        }
        int last = LinearLocationMath.FindLastUsableComponent(components);
        if (target >= total)
        {
            return LinearLocationMath.CreateLocation(
                last, components[last].Length - 1, 0.0);
        }

        double traversed = 0.0;
        int componentIndex = 0;
        while (componentIndex < components.Length)
        {
            Coordinate[] component = components[componentIndex];
            int segmentIndex = 0;
            while (segmentIndex + 1 < component.Length)
            {
                double segmentLength = GeographicMath.GetCentralAngle(
                    component[segmentIndex], component[segmentIndex + 1]);
                double segmentEnd = traversed + segmentLength;
                if (segmentLength > 0.0 && target <= segmentEnd)
                {
                    double fraction =
                        (target - traversed) / segmentLength;
                    if (fraction >= 1.0)
                    {
                        return LinearLocationMath.CreateLocation(
                            componentIndex, segmentIndex + 1, 0.0);
                    }
                    return LinearLocationMath.CreateLocation(
                        componentIndex, segmentIndex, fraction);
                }
                traversed = segmentEnd;
                segmentIndex++;
            }
            componentIndex++;
        }
        return LinearLocationMath.CreateLocation(
            last, components[last].Length - 1, 0.0);
    }

    internal static Coordinate? GetCoordinateAtLocation(
        Coordinate[][] components, LinearLocation location)
    {
        if (!location.IsValid) return null;
        Coordinate[] component = components[location.ComponentIndex];
        Coordinate start = component[location.SegmentIndex];
        if (location.SegmentFraction == 0.0 ||
            location.SegmentIndex + 1 >= component.Length)
        {
            return CoordinateSequences.CopyCoordinateValue(start);
        }
        return InterpolateGreatCircle(
            start, component[location.SegmentIndex + 1],
            location.SegmentFraction);
    }

    internal static List<LineString> CreateSliceLines(
        Coordinate[][] components, LinearLocation start,
        LinearLocation end)
    {
        List<LineString> lines = new List<LineString>();
        int componentIndex = start.ComponentIndex;
        while (componentIndex <= end.ComponentIndex)
        {
            Coordinate[] component = components[componentIndex];
            if (component.Length > 0)
            {
                LinearLocation componentStart = componentIndex ==
                    start.ComponentIndex ? start :
                    LinearLocationMath.CreateLocation(
                        componentIndex, 0, 0.0);
                LinearLocation componentEnd = componentIndex ==
                    end.ComponentIndex ? end :
                    LinearLocationMath.CreateLocation(
                        componentIndex, component.Length - 1, 0.0);
                lines.Add(CreateSliceLine(
                    components, componentStart, componentEnd));
            }
            componentIndex++;
        }
        return lines;
    }

    internal static LineString CreateSliceLine(
        Coordinate[][] components, LinearLocation start,
        LinearLocation end)
    {
        Coordinate[] source = components[start.ComponentIndex];
        List<Coordinate> coordinates = new List<Coordinate>();
        Coordinate? startCoordinate = GetCoordinateAtLocation(
            components, start);
        Coordinate? endCoordinate = GetCoordinateAtLocation(
            components, end);
        if (startCoordinate == null || endCoordinate == null)
        {
            throw new InvalidGeometryException(
                "Geographic slice endpoints must be valid.");
        }
        coordinates.Add(startCoordinate);
        int vertexIndex = start.SegmentIndex + 1;
        while (vertexIndex < end.SegmentIndex ||
            (vertexIndex == end.SegmentIndex &&
                end.SegmentFraction > 0.0))
        {
            coordinates.Add(CoordinateSequences.CopyCoordinateValue(
                source[vertexIndex]));
            vertexIndex++;
        }
        coordinates.Add(endCoordinate);
        LineString line = new LineString();
        line.Coordinates = coordinates.ToArray();
        return line;
    }

    internal static double ClampDistance(double distance, double maximum)
    {
        if (distance < 0.0) return 0.0;
        if (distance > maximum) return maximum;
        return distance;
    }

    internal static double GetMercatorLatitude(double latitude)
    {
        double limit = Math.PI / 2.0 - 1.0e-15;
        double safe = GeographicMath.Clamp(latitude, -limit, limit);
        return Math.Log(Math.Tan(Math.PI / 4.0 + safe / 2.0));
    }

    internal static double NormalizeLatitudeRadians(double latitude)
    {
        double degrees = GeographicMath.RadiansToDegrees(latitude);
        return GeographicMath.DegreesToRadians(
            GeographicMath.NormalizeLatitude(degrees));
    }

    internal static Coordinate CreateNormalizedCopy(Coordinate source)
    {
        Coordinate result =
            CoordinateSequences.CopyCoordinateValue(source);
        result.X = GeographicMath.NormalizeLongitude(source.X);
        return result;
    }

    internal static double[] ToUnitVector(Coordinate coordinate)
    {
        double latitude = GeographicMath.DegreesToRadians(coordinate.Y);
        double longitude = GeographicMath.DegreesToRadians(
            GeographicMath.NormalizeLongitude(coordinate.X));
        double latitudeCosine = Math.Cos(latitude);
        return
        [
            latitudeCosine * Math.Cos(longitude),
            latitudeCosine * Math.Sin(longitude),
            Math.Sin(latitude)
        ];
    }

    internal static Coordinate FromUnitVector(double[] vector)
    {
        Coordinate result = new Coordinate();
        result.X = GeographicMath.NormalizeLongitude(
            GeographicMath.RadiansToDegrees(
                Math.Atan2(vector[1], vector[0])));
        result.Y = GeographicMath.RadiansToDegrees(Math.Atan2(
            vector[2], Math.Sqrt(
                vector[0] * vector[0] + vector[1] * vector[1])));
        return result;
    }

    internal static double[] CrossVector(double[] first, double[] second)
    {
        return
        [
            first[1] * second[2] - first[2] * second[1],
            first[2] * second[0] - first[0] * second[2],
            first[0] * second[1] - first[1] * second[0]
        ];
    }

    internal static double DotVector(double[] first, double[] second)
    {
        return first[0] * second[0] +
            first[1] * second[1] +
            first[2] * second[2];
    }

    internal static double VectorAngle(double[] first, double[] second)
    {
        double[] cross = CrossVector(first, second);
        double crossLength = Math.Sqrt(DotVector(cross, cross));
        return Math.Atan2(crossLength, DotVector(first, second));
    }

    internal static double[] NormalizeVector(double[] vector)
    {
        double length = Math.Sqrt(DotVector(vector, vector));
        if (length <= 1.0e-30) return [];
        return
        [
            vector[0] / length,
            vector[1] / length,
            vector[2] / length
        ];
    }
}

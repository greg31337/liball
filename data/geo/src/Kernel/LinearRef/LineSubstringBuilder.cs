using System.Collections.Generic;

namespace geo;

internal static class LineSubstringBuilder
{
    internal static Geometry ExtractLine(
        Geometry geometry, LinearLocation start,
        LinearLocation end)
    {
        Coordinate[][] components =
            LinearLocationMath.GetLineComponents(geometry);
        LinearLocation normalizedStart =
            LinearLocationMath.NormalizeLocation(geometry, start);
        LinearLocation normalizedEnd =
            LinearLocationMath.NormalizeLocation(geometry, end);
        if (!normalizedStart.IsValid || !normalizedEnd.IsValid)
        {
            return CreateLineGeometry(geometry, []);
        }
        bool reverse = LinearLocationMath.CompareLocations(
            normalizedStart, normalizedEnd) > 0;
        LinearLocation lower = reverse ? normalizedEnd : normalizedStart;
        LinearLocation upper = reverse ? normalizedStart : normalizedEnd;
        List<LineString> lines = new List<LineString>();
        int componentIndex = lower.ComponentIndex;
        while (componentIndex <= upper.ComponentIndex)
        {
            Coordinate[] coordinates = components[componentIndex];
            if (coordinates.Length > 0)
            {
                LinearLocation componentStart = componentIndex ==
                    lower.ComponentIndex ? lower :
                    LinearLocationMath.CreateLocation(
                        componentIndex, 0, 0.0);
                LinearLocation componentEnd = componentIndex ==
                    upper.ComponentIndex ? upper :
                    LinearLocationMath.CreateLocation(
                        componentIndex, coordinates.Length - 1, 0.0);
                lines.Add(CreateSubstringLine(
                    geometry, coordinates,
                    componentStart, componentEnd));
            }
            componentIndex++;
        }
        if (reverse) ReverseLines(lines);
        return CreateResultGeometry(geometry, lines);
    }

    internal static LineString CreateSubstringLine(
        Geometry geometry, Coordinate[] coordinates,
        LinearLocation start, LinearLocation end)
    {
        List<Coordinate> result = new List<Coordinate>();
        Coordinate? startCoordinate =
            LinearLocationMath.GetCoordinateAtLocation(geometry, start);
        Coordinate? endCoordinate =
            LinearLocationMath.GetCoordinateAtLocation(geometry, end);
        if (startCoordinate == null || endCoordinate == null)
        {
            throw new InvalidGeometryException(
                "Line substring endpoints must be valid.");
        }
        result.Add(startCoordinate);
        int vertexIndex = start.SegmentIndex + 1;
        while (vertexIndex < end.SegmentIndex ||
            (vertexIndex == end.SegmentIndex &&
                end.SegmentFraction > 0.0))
        {
            result.Add(CoordinateSequences.CopyCoordinateValue(
                coordinates[vertexIndex]));
            vertexIndex++;
        }
        result.Add(endCoordinate);
        LineString line = new LineString();
        line.Coordinates = result.ToArray();
        return line;
    }

    internal static void ReverseLines(List<LineString> lines)
    {
        int left = 0;
        int right = lines.Count - 1;
        while (left < right)
        {
            LineString value = lines[left];
            lines[left] = lines[right];
            lines[right] = value;
            left++;
            right--;
        }
        int lineIndex = 0;
        while (lineIndex < lines.Count)
        {
            ReverseCoordinates(lines[lineIndex].Coordinates);
            lineIndex++;
        }
    }

    internal static void ReverseCoordinates(Coordinate[] coordinates)
    {
        int left = 0;
        int right = coordinates.Length - 1;
        while (left < right)
        {
            Coordinate value = coordinates[left];
            coordinates[left] = coordinates[right];
            coordinates[right] = value;
            left++;
            right--;
        }
    }

    internal static Geometry CreateResultGeometry(
        Geometry source, List<LineString> lines)
    {
        if (lines.Count == 0)
        {
            return CreateLineGeometry(source, []);
        }
        if (lines.Count == 1)
        {
            return CreateLineGeometry(
                source, lines[0].Coordinates);
        }
        Geometry result = CreateGeometryShell(
            source, GeometryType.MultiLineString);
        MultiLineString multiLine = new MultiLineString();
        multiLine.LineStrings = lines.ToArray();
        result.MultiLineString = multiLine;
        return result;
    }

    internal static Geometry CreateLineGeometry(
        Geometry source, Coordinate[] coordinates)
    {
        Geometry result = CreateGeometryShell(
            source, GeometryType.LineString);
        LineString line = new LineString();
        line.Coordinates = coordinates;
        result.LineString = line;
        return result;
    }

    internal static Geometry CreateGeometryShell(
        Geometry source, GeometryType geometryType)
    {
        Geometry result = new Geometry();
        result.Point = null;
        result.GeometryType = geometryType;
        result.CoordinateLayout = source.CoordinateLayout;
        result.SpatialReferenceId = source.SpatialReferenceId;
        return result;
    }
}

using System.Collections.Generic;

namespace geo;

internal static class CoordinateSequences
{
    internal static Coordinate[] GetDirectCoordinates(Geometry geometry)
    {
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
                Point point = (Point)payload;
                if (point.Coordinate == null) return [];
                return [point.Coordinate];
            case GeometryType.LineString:
                return ((LineString)payload).Coordinates;
            case GeometryType.LinearRing:
                return ((LinearRing)payload).Coordinates;
            case GeometryType.CircularString:
                return ((CircularString)payload).Coordinates;
            default:
                return [];
        }
    }

    internal static Coordinate[] ExtractCoordinates(Geometry geometry, int maximumDepth, int maximumComponents)
    {
        Geometry[] leaves = ComponentWalker.FlattenLeafGeometries(geometry, maximumDepth, maximumComponents);
        List<Coordinate> coordinates = new List<Coordinate>();
        foreach (Geometry leaf in leaves)
        {
            AppendLeafCoordinates(leaf, coordinates);
        }

        return coordinates.ToArray();
    }

    internal static Coordinate CopyCoordinateValue(Coordinate coordinate)
    {
        if (coordinate == null)
        {
            throw new InvalidGeometryException("Coordinate is null.");
        }

        Coordinate copy = new Coordinate();
        copy.X = coordinate.X;
        copy.Y = coordinate.Y;
        copy.Z = coordinate.Z;
        copy.M = coordinate.M;
        return copy;
    }

    internal static Coordinate[] CopyCoordinateSequence(Coordinate[] coordinates)
    {
        if (coordinates == null)
        {
            throw new InvalidGeometryException("Coordinate sequence is null.");
        }

        Coordinate[] copy = new Coordinate[coordinates.Length];
        int index = 0;
        while (index < coordinates.Length)
        {
            copy[index] = CopyCoordinateValue(coordinates[index]);
            index++;
        }

        return copy;
    }

    internal static void AppendLeafCoordinates(Geometry geometry, List<Coordinate> coordinates)
    {
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
                Point point = (Point)payload;
                if (point.Coordinate != null) coordinates.Add(point.Coordinate);
                break;
            case GeometryType.LineString:
                AppendCoordinateArray(((LineString)payload).Coordinates, coordinates);
                break;
            case GeometryType.LinearRing:
                AppendCoordinateArray(((LinearRing)payload).Coordinates, coordinates);
                break;
            case GeometryType.Polygon:
                AppendPolygonCoordinates((Polygon)payload, coordinates);
                break;
            case GeometryType.Triangle:
                Triangle triangle = (Triangle)payload;
                if (triangle.Shell == null)
                {
                    throw new InvalidGeometryException("Triangle shell is null.");
                }
                AppendCoordinateArray(triangle.Shell.Coordinates, coordinates);
                break;
            case GeometryType.CircularString:
                AppendCoordinateArray(((CircularString)payload).Coordinates, coordinates);
                break;
            default:
                break;
        }
    }

    internal static void AppendPolygonCoordinates(Polygon polygon, List<Coordinate> coordinates)
    {
        if (polygon.Shell == null)
        {
            throw new InvalidGeometryException("Polygon shell is null.");
        }

        AppendCoordinateArray(polygon.Shell.Coordinates, coordinates);
        if (polygon.Holes == null)
        {
            throw new InvalidGeometryException("Polygon hole array is null.");
        }

        foreach (LinearRing hole in polygon.Holes)
        {
            if (hole == null)
            {
                throw new InvalidGeometryException("Polygon hole is null.");
            }

            AppendCoordinateArray(hole.Coordinates, coordinates);
        }
    }

    internal static void AppendCoordinateArray(Coordinate[] source, List<Coordinate> destination)
    {
        if (source == null)
        {
            throw new InvalidGeometryException("Coordinate sequence is null.");
        }

        foreach (Coordinate coordinate in source)
        {
            if (coordinate == null)
            {
                throw new InvalidGeometryException("Coordinate is null.");
            }

            destination.Add(coordinate);
        }
    }
}

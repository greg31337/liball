using System.Collections.Generic;

namespace geo;

internal static class ConvexHullAlgorithm
{
    internal static Geometry GetConvexHull(Geometry geometry)
    {
        Coordinate[] source =
            GeometryInspection.ExtractValidatedCoordinates(geometry);
        GeometryFactoryOptions options =
            GeometryNormalization.CreateNormalizationOptions(geometry);
        Coordinate[] coordinates = SortAndRemovePlanarDuplicates(source);
        if (coordinates.Length == 0)
        {
            return GeometryBuilder.CreateEmptyValue(
                GeometryType.GeometryCollection, options);
        }
        if (coordinates.Length == 1)
        {
            return GeometryBuilder.CreatePointValue(
                coordinates[0], options);
        }

        Coordinate[] hull = BuildMonotoneHull(coordinates);
        if (hull.Length == 1)
        {
            return GeometryBuilder.CreatePointValue(hull[0], options);
        }
        if (hull.Length == 2)
        {
            return GeometryBuilder.CreateLineStringValue(hull, options);
        }

        Coordinate[] ringCoordinates = new Coordinate[hull.Length + 1];
        int index = 0;
        while (index < hull.Length)
        {
            ringCoordinates[index] =
                RobustLineIntersection.CopyCoordinate(hull[index]);
            index++;
        }
        ringCoordinates[hull.Length] =
            RobustLineIntersection.CopyCoordinate(hull[0]);
        LinearRing shell = new LinearRing();
        shell.Coordinates = ringCoordinates;
        return GeometryBuilder.CreatePolygonValue(shell, [], options);
    }

    internal static Coordinate[] SortAndRemovePlanarDuplicates(
        Coordinate[] source)
    {
        List<Coordinate> sorted = new List<Coordinate>();
        int index = 0;
        while (index < source.Length)
        {
            sorted.Add(RobustLineIntersection.CopyCoordinate(source[index]));
            index++;
        }
        sorted.Sort((first, second) =>
            CompareHullCoordinates(first, second));

        List<Coordinate> unique = new List<Coordinate>();
        index = 0;
        while (index < sorted.Count)
        {
            if (unique.Count == 0 ||
                !RobustLineIntersection.CoordinatesEqualPlanar(
                    unique[unique.Count - 1], sorted[index]))
            {
                unique.Add(sorted[index]);
            }
            index++;
        }
        return unique.ToArray();
    }

    internal static int CompareHullCoordinates(
        Coordinate first, Coordinate second)
    {
        int planar = RobustLineIntersection.ComparePlanarCoordinates(
            first, second);
        if (planar != 0) return planar;
        int height = CompareOptionalOrdinates(first.Z, second.Z);
        if (height != 0) return height;
        return CompareOptionalOrdinates(first.M, second.M);
    }

    internal static int CompareOptionalOrdinates(
        double first, double second)
    {
        bool firstMissing = double.IsNaN(first);
        bool secondMissing = double.IsNaN(second);
        if (firstMissing && secondMissing) return 0;
        if (firstMissing) return -1;
        if (secondMissing) return 1;
        if (first < second) return -1;
        if (first > second) return 1;
        return 0;
    }

    internal static Coordinate[] BuildMonotoneHull(Coordinate[] sorted)
    {
        List<Coordinate> lower = new List<Coordinate>();
        int index = 0;
        while (index < sorted.Length)
        {
            AddHullCoordinate(lower, sorted[index]);
            index++;
        }

        List<Coordinate> upper = new List<Coordinate>();
        index = sorted.Length - 1;
        while (index >= 0)
        {
            AddHullCoordinate(upper, sorted[index]);
            index--;
        }

        lower.RemoveAt(lower.Count - 1);
        upper.RemoveAt(upper.Count - 1);
        List<Coordinate> hull = new List<Coordinate>();
        AppendHullCoordinates(hull, lower);
        AppendHullCoordinates(hull, upper);
        return hull.ToArray();
    }

    internal static void AddHullCoordinate(
        List<Coordinate> hull, Coordinate coordinate)
    {
        while (hull.Count >= 2 &&
            RobustOrientation.GetOrientationIndex(
                hull[hull.Count - 2], hull[hull.Count - 1],
                coordinate) <= 0)
        {
            hull.RemoveAt(hull.Count - 1);
        }
        hull.Add(coordinate);
    }

    internal static void AppendHullCoordinates(
        List<Coordinate> destination, List<Coordinate> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index++;
        }
    }
}

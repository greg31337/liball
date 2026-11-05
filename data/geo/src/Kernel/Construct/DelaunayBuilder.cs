using System;
using System.Collections.Generic;

namespace geo;

internal class TriangulationEdge
{
    internal int First;
    internal int Second;
    internal int Count = 1;
}

internal class TriangulationTriangle
{
    internal int First;
    internal int Second;
    internal int Third;
}

internal class DelaunayMesh
{
    internal Coordinate[] Sites = [];
    internal List<TriangulationTriangle> Triangles =
        new List<TriangulationTriangle>();
}

internal static class DelaunayBuilder
{
    internal static Geometry GetDelaunayTriangulation(Geometry geometry)
    {
        Coordinate[] sites = ExtractUniqueSites(geometry);
        DelaunayMesh mesh = BuildMesh(sites);
        GeometryFactoryOptions options =
            GeometryNormalization.CreateNormalizationOptions(geometry);
        return CreateTinValue(mesh.Sites, mesh.Triangles, options);
    }

    internal static Coordinate[] ExtractUniqueSites(Geometry geometry)
    {
        Coordinate[] source =
            GeometryInspection.ExtractValidatedCoordinates(geometry);
        Coordinate[] sites =
            ConvexHullAlgorithm.SortAndRemovePlanarDuplicates(source);
        if (sites.Length > GeometryConstants.DefaultMaximumCoordinates)
        {
            throw new InvalidGeometryException(
                "Triangulation site count exceeds the supported limit.");
        }
        return sites;
    }

    internal static DelaunayMesh BuildMesh(Coordinate[] sites)
    {
        DelaunayMesh result = new DelaunayMesh();
        result.Sites = sites;
        if (sites.Length < 3) return result;

        Coordinate[] working = CreateWorkingSites(sites);
        int firstSuper = sites.Length;
        List<TriangulationTriangle> triangles =
            new List<TriangulationTriangle>();
        triangles.Add(CreateCounterClockwiseTriangle(
            firstSuper, firstSuper + 1, firstSuper + 2, working));

        int siteIndex = 0;
        while (siteIndex < sites.Length)
        {
            List<TriangulationTriangle> removed =
                new List<TriangulationTriangle>();
            int triangleIndex = 0;
            while (triangleIndex < triangles.Count)
            {
                TriangulationTriangle triangle = triangles[triangleIndex];
                if (IsInsideCircumcircle(
                    working[triangle.First], working[triangle.Second],
                    working[triangle.Third], working[siteIndex]))
                {
                    removed.Add(triangle);
                }
                triangleIndex++;
            }

            List<TriangulationEdge> boundary =
                new List<TriangulationEdge>();
            triangleIndex = 0;
            while (triangleIndex < removed.Count)
            {
                TriangulationTriangle triangle = removed[triangleIndex];
                AddBoundaryEdge(boundary, triangle.First, triangle.Second);
                AddBoundaryEdge(boundary, triangle.Second, triangle.Third);
                AddBoundaryEdge(boundary, triangle.Third, triangle.First);
                triangles.Remove(triangle);
                triangleIndex++;
            }

            int edgeIndex = 0;
            while (edgeIndex < boundary.Count)
            {
                TriangulationEdge edge = boundary[edgeIndex];
                if (edge.Count == 1 && RobustOrientation.GetOrientationIndex(
                    working[edge.First], working[edge.Second],
                    working[siteIndex]) != 0)
                {
                    triangles.Add(CreateCounterClockwiseTriangle(
                        edge.First, edge.Second, siteIndex, working));
                }
                edgeIndex++;
            }
            siteIndex++;
        }

        int index = triangles.Count - 1;
        while (index >= 0)
        {
            TriangulationTriangle triangle = triangles[index];
            if (triangle.First >= firstSuper ||
                triangle.Second >= firstSuper ||
                triangle.Third >= firstSuper)
            {
                triangles.RemoveAt(index);
            }
            index--;
        }
        SortTriangles(triangles);
        result.Triangles = triangles;
        return result;
    }

    internal static Coordinate[] CreateWorkingSites(Coordinate[] sites)
    {
        double minimumX = sites[0].X;
        double maximumX = sites[0].X;
        double minimumY = sites[0].Y;
        double maximumY = sites[0].Y;
        int index = 1;
        while (index < sites.Length)
        {
            Coordinate site = sites[index];
            if (site.X < minimumX) minimumX = site.X;
            if (site.X > maximumX) maximumX = site.X;
            if (site.Y < minimumY) minimumY = site.Y;
            if (site.Y > maximumY) maximumY = site.Y;
            index++;
        }
        double spanX = maximumX - minimumX;
        double spanY = maximumY - minimumY;
        double span = spanX > spanY ? spanX : spanY;
        if (!double.IsFinite(span) || span <= 0.0)
        {
            throw new InvalidGeometryException(
                "Triangulation site extent must be finite and non-zero.");
        }
        double centerX = minimumX + spanX / 2.0;
        double centerY = minimumY + spanY / 2.0;
        double offset = span * 32.0;
        if (!double.IsFinite(centerX) || !double.IsFinite(centerY) ||
            !double.IsFinite(offset) ||
            !double.IsFinite(centerX - offset * 2.0) ||
            !double.IsFinite(centerX + offset * 2.0) ||
            !double.IsFinite(centerY - offset) ||
            !double.IsFinite(centerY + offset * 2.0))
        {
            throw new InvalidGeometryException(
                "Triangulation site extent is outside the finite working range.");
        }

        Coordinate[] working = new Coordinate[sites.Length + 3];
        index = 0;
        while (index < sites.Length)
        {
            working[index] = sites[index];
            index++;
        }
        working[sites.Length] = CreatePlanarCoordinate(
            centerX - offset * 2.0, centerY - offset);
        working[sites.Length + 1] = CreatePlanarCoordinate(
            centerX, centerY + offset * 2.0);
        working[sites.Length + 2] = CreatePlanarCoordinate(
            centerX + offset * 2.0, centerY - offset);
        return working;
    }

    internal static TriangulationTriangle CreateCounterClockwiseTriangle(
        int first, int second, int third, Coordinate[] coordinates)
    {
        TriangulationTriangle triangle = new TriangulationTriangle();
        int orientation = RobustOrientation.GetOrientationIndex(
            coordinates[first], coordinates[second], coordinates[third]);
        if (orientation > 0)
        {
            triangle.First = first;
            triangle.Second = second;
        }
        else
        {
            triangle.First = second;
            triangle.Second = first;
        }
        triangle.Third = third;
        return triangle;
    }

    internal static bool IsInsideCircumcircle(
        Coordinate first, Coordinate second,
        Coordinate third, Coordinate point)
    {
        return GetCircumcircleSign(
            first, second, third, point) >= 0;
    }

    internal static int GetCircumcircleSign(
        Coordinate first, Coordinate second,
        Coordinate third, Coordinate point)
    {
        double scale = GetCircumcircleScale(
            first, second, third, point);
        DoubleDouble firstX = CreateScaledDifference(first.X, point.X, scale);
        DoubleDouble firstY = CreateScaledDifference(first.Y, point.Y, scale);
        DoubleDouble secondX = CreateScaledDifference(second.X, point.X, scale);
        DoubleDouble secondY = CreateScaledDifference(second.Y, point.Y, scale);
        DoubleDouble thirdX = CreateScaledDifference(third.X, point.X, scale);
        DoubleDouble thirdY = CreateScaledDifference(third.Y, point.Y, scale);
        DoubleDouble firstLength = firstX.MultiplyValue(firstX).AddValue(
            firstY.MultiplyValue(firstY));
        DoubleDouble secondLength = secondX.MultiplyValue(secondX).AddValue(
            secondY.MultiplyValue(secondY));
        DoubleDouble thirdLength = thirdX.MultiplyValue(thirdX).AddValue(
            thirdY.MultiplyValue(thirdY));
        DoubleDouble firstCross = secondX.MultiplyValue(thirdY).SubtractValue(
            secondY.MultiplyValue(thirdX));
        DoubleDouble secondCross = firstX.MultiplyValue(thirdY).SubtractValue(
            firstY.MultiplyValue(thirdX));
        DoubleDouble thirdCross = firstX.MultiplyValue(secondY).SubtractValue(
            firstY.MultiplyValue(secondX));
        DoubleDouble determinant = firstLength.MultiplyValue(firstCross)
            .SubtractValue(secondLength.MultiplyValue(secondCross))
            .AddValue(thirdLength.MultiplyValue(thirdCross));
        return determinant.CompareToZero();
    }

    internal static double GetCircumcircleScale(
        Coordinate first, Coordinate second,
        Coordinate third, Coordinate point)
    {
        double scale = 1.0;
        scale = SelectDifferenceMagnitude(scale, first.X, point.X);
        scale = SelectDifferenceMagnitude(scale, first.Y, point.Y);
        scale = SelectDifferenceMagnitude(scale, second.X, point.X);
        scale = SelectDifferenceMagnitude(scale, second.Y, point.Y);
        scale = SelectDifferenceMagnitude(scale, third.X, point.X);
        scale = SelectDifferenceMagnitude(scale, third.Y, point.Y);
        return scale;
    }

    internal static double SelectDifferenceMagnitude(
        double current, double first, double second)
    {
        double difference = first - second;
        if (double.IsFinite(difference))
        {
            return SelectLargerMagnitude(current, difference);
        }
        current = SelectLargerMagnitude(current, first);
        return SelectLargerMagnitude(current, second);
    }

    internal static double SelectLargerMagnitude(double current, double value)
    {
        double magnitude = Math.Abs(value);
        return magnitude > current ? magnitude : current;
    }

    internal static DoubleDouble CreateScaledDifference(
        double first, double second, double scale)
    {
        double difference = first - second;
        if (double.IsFinite(difference))
        {
            return new DoubleDouble(difference / scale, 0.0);
        }
        DoubleDouble firstValue = new DoubleDouble(first / scale, 0.0);
        DoubleDouble secondValue = new DoubleDouble(second / scale, 0.0);
        return firstValue.SubtractValue(secondValue);
    }

    internal static void AddBoundaryEdge(
        List<TriangulationEdge> edges, int first, int second)
    {
        int minimum = first < second ? first : second;
        int maximum = first < second ? second : first;
        int index = 0;
        while (index < edges.Count)
        {
            TriangulationEdge edge = edges[index];
            if (edge.First == minimum && edge.Second == maximum)
            {
                edge.Count++;
                return;
            }
            index++;
        }
        TriangulationEdge added = new TriangulationEdge();
        added.First = minimum;
        added.Second = maximum;
        edges.Add(added);
    }

    internal static void SortTriangles(List<TriangulationTriangle> triangles)
    {
        triangles.Sort((first, second) =>
            CompareTriangles(first, second));
    }

    internal static int CompareTriangles(
        TriangulationTriangle first, TriangulationTriangle second)
    {
        int firstMinimum = GetMinimumIndex(first);
        int secondMinimum = GetMinimumIndex(second);
        if (firstMinimum < secondMinimum) return -1;
        if (firstMinimum > secondMinimum) return 1;
        int firstMaximum = GetMaximumIndex(first);
        int secondMaximum = GetMaximumIndex(second);
        if (firstMaximum < secondMaximum) return -1;
        if (firstMaximum > secondMaximum) return 1;
        int firstMiddle = first.First + first.Second + first.Third -
            firstMinimum - firstMaximum;
        int secondMiddle = second.First + second.Second + second.Third -
            secondMinimum - secondMaximum;
        if (firstMiddle < secondMiddle) return -1;
        if (firstMiddle > secondMiddle) return 1;
        return 0;
    }

    internal static int GetMinimumIndex(TriangulationTriangle triangle)
    {
        int minimum = triangle.First < triangle.Second ?
            triangle.First : triangle.Second;
        return triangle.Third < minimum ? triangle.Third : minimum;
    }

    internal static int GetMaximumIndex(TriangulationTriangle triangle)
    {
        int maximum = triangle.First > triangle.Second ?
            triangle.First : triangle.Second;
        return triangle.Third > maximum ? triangle.Third : maximum;
    }

    internal static Geometry CreateTinValue(
        Coordinate[] coordinates,
        List<TriangulationTriangle> triangles,
        GeometryFactoryOptions options)
    {
        Geometry result = GeometryBuilder.CreateGeometryShell(
            GeometryType.Tin, options);
        Tin tin = new Tin();
        tin.Triangles = new Triangle[triangles.Count];
        int index = 0;
        while (index < triangles.Count)
        {
            TriangulationTriangle source = triangles[index];
            Triangle triangle = new Triangle();
            triangle.Shell.Coordinates =
            [
                GeometryBuilder.CopyCoordinateForFactory(
                    coordinates[source.First], options),
                GeometryBuilder.CopyCoordinateForFactory(
                    coordinates[source.Second], options),
                GeometryBuilder.CopyCoordinateForFactory(
                    coordinates[source.Third], options),
                GeometryBuilder.CopyCoordinateForFactory(
                    coordinates[source.First], options)
            ];
            tin.Triangles[index] = triangle;
            index++;
        }
        result.Tin = tin;
        return result;
    }

    internal static Coordinate CreatePlanarCoordinate(double x, double y)
    {
        Coordinate coordinate = new Coordinate();
        coordinate.X = x;
        coordinate.Y = y;
        return coordinate;
    }
}
